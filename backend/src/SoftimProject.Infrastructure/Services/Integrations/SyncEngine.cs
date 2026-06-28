using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Common;
using SoftimProject.Application.Integrations;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Services.Integrations;

/// <summary>
/// Provider-agnostic sync/import engine. Consumes an <see cref="ISourceConnector"/> and
/// the canonical model, and upserts ProjectMan entities idempotently (keyed by external
/// id). This is the extraction of the former EasyProject-specific migration logic; for
/// the EasyProject connector it reproduces the exact prior behavior (same external-system
/// strings, synthetic user keys, comment source, URL shape and fallbacks), so the
/// one-time migration is unchanged. Future systems (Jira, Redmine) reuse it as-is.
/// </summary>
public sealed class SyncEngine(
    IApplicationDbContext dbContext,
    IMigrationProgressTracker tracker,
    IBlobStorageService blobStorage,
    ILogger<SyncEngine> logger)
{
    public async Task ExecuteAsync(Guid jobId, Guid adminUserId, SyncEngineRequest request, ISourceConnector connector, SourceConnectionContext context, ISyncJobSink sink)
    {
        // Provider-derived literals. For EasyProject these resolve to exactly the values
        // the old service used ("EasyProject", "easyproject:{id}", CommentSource.EasyProject).
        var systemName = connector.SourceSystem.ToString();
        var userPrefix = systemName.ToLowerInvariant();
        var commentSource = MapCommentSource(connector.SourceSystem);

        try
        {
            var ct = tracker.GetCancellationToken(jobId);
            // Default TaskState/TicketPriority resolved from the target template's states,
            // not globally — a ticket without an explicit source status must land in a
            // state that actually belongs to its project (and therefore template).
            var defaultStateId = await dbContext.TaskStates
                .Where(ts => ts.IsActive && ts.IsDefault && ts.ProjectTemplateId == request.TargetProjectTemplateId)
                .Select(ts => ts.Id)
                .FirstOrDefaultAsync(ct);
            if (defaultStateId == Guid.Empty)
                defaultStateId = await dbContext.TaskStates
                    .Where(ts => ts.IsActive && ts.ProjectTemplateId == request.TargetProjectTemplateId)
                    .OrderBy(ts => ts.SortOrder)
                    .Select(ts => ts.Id)
                    .FirstAsync(ct);

            var defaultPriorityId = await dbContext.TicketPriorities
                .Where(tp => tp.IsActive && tp.IsDefault && tp.ProjectTemplateId == request.TargetProjectTemplateId)
                .Select(tp => tp.Id)
                .FirstOrDefaultAsync(ct);
            if (defaultPriorityId == Guid.Empty)
                defaultPriorityId = await dbContext.TicketPriorities
                    .Where(tp => tp.IsActive && tp.ProjectTemplateId == request.TargetProjectTemplateId)
                    .OrderBy(tp => tp.SortOrder)
                    .Select(tp => tp.Id)
                    .FirstAsync(ct);

            // Phase 1: Fetch data from the source system (via the connector → canonical model)
            tracker.UpdatePhase(jobId, "Fetching data from source");
            tracker.AddLog(jobId, "Starting data fetch...");
            await sink.NotifyAsync(tracker.GetProgress(jobId));

            var allProjects = await connector.GetProjectsAsync(context, ct);
            var selectedProjects = new List<CanonicalProject>();
            var issuesByProject = new Dictionary<string, List<CanonicalIssue>>();
            var worklogsByProject = new Dictionary<string, List<CanonicalWorklog>>();

            foreach (var externalId in request.ProjectExternalIds)
            {
                ct.ThrowIfCancellationRequested();
                var project = allProjects.FirstOrDefault(p => p.ExternalId == externalId);
                if (project is null) continue;
                selectedProjects.Add(project);

                var issues = (await connector.GetIssuesAsync(context, project.ExternalId, request.ChangedSince, ct)).ToList();
                issuesByProject[project.ExternalId] = issues;

                if (request.ImportWorklogs)
                {
                    var worklogs = (await connector.GetWorklogsAsync(context, project.ExternalId, request.ChangedSince, ct)).ToList();
                    worklogsByProject[project.ExternalId] = worklogs;
                }

                tracker.AddLog(jobId, $"Fetched project '{project.Name}': {issues.Count} issues");
            }
            await sink.AdvancePhaseAsync(MigrationPhase.Fetching, ct);

            // Phase 2: Lookups
            tracker.UpdatePhase(jobId, "Processing lookups");
            await sink.NotifyAsync(tracker.GetProgress(jobId));

            var taskTypeMap = await EnsureTaskTypes(jobId, request.TrackerMapping, request.AutoCreateTrackers, ct);
            var taskStateMap = await EnsureTaskStates(jobId, request.TargetProjectTemplateId, request.AutoCreateStatuses, request.AutoCreateStatusIsClosed, ct);
            var priorityMap = await EnsureTicketPriorities(jobId, request.TargetProjectTemplateId, request.AutoCreatePriorities, ct);

            // Merge auto-created into existing mappings
            var mergedStatusMapping = new Dictionary<string, Guid>(request.StatusMapping);
            foreach (var (epId, spId) in taskStateMap)
                mergedStatusMapping[epId] = spId;

            var mergedPriorityMapping = new Dictionary<string, Guid>(request.PriorityMapping);
            foreach (var (epId, spId) in priorityMap)
                mergedPriorityMapping[epId] = spId;
            await sink.AdvancePhaseAsync(MigrationPhase.Lookups, ct);

            // Phase 3: Users
            tracker.UpdatePhase(jobId, "Processing users");
            await sink.NotifyAsync(tracker.GetProgress(jobId));

            var userMap = await EnsureUsers(jobId, request, connector, context, userPrefix, ct);
            await sink.AdvancePhaseAsync(MigrationPhase.Users, ct);

            // Phase 4: Projects
            tracker.UpdatePhase(jobId, "Migrating projects");
            tracker.UpdateCounts(jobId, "projects", selectedProjects.Count, 0);
            await sink.NotifyAsync(tracker.GetProgress(jobId));

            var projectMap = new Dictionary<string, Guid>();
            var projectsMigrated = 0;

            foreach (var project in selectedProjects)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var spProjectId = await MigrateProject(jobId, project, context.BaseUrl, request.TargetProjectTemplateId, adminUserId, systemName, request.IntegrationConnectionId, request.TargetCompanyId, ct);
                    projectMap[project.ExternalId] = spProjectId;
                    projectsMigrated++;
                    tracker.UpdateCounts(jobId, "projects", selectedProjects.Count, projectsMigrated);
                }
                catch (Exception ex)
                {
                    tracker.AddError(jobId, $"Failed to migrate project '{project.Name}': {ex.Message}");
                    logger.LogError(ex, "Failed to migrate project {ExternalId}", project.ExternalId);
                }
            }

            foreach (var project in selectedProjects.Where(p => p.ParentExternalId != null))
            {
                if (projectMap.TryGetValue(project.ExternalId, out var childId) &&
                    projectMap.TryGetValue(project.ParentExternalId!, out var parentId))
                {
                    var child = await dbContext.Projects.FindAsync([childId], ct);
                    if (child != null) child.ParentProjectId = parentId;
                }
            }
            await dbContext.SaveChangesAsync(ct);
            await sink.AdvancePhaseAsync(MigrationPhase.Projects, ct);

            // Phase 5: Tickets
            tracker.UpdatePhase(jobId, "Migrating tickets");
            var totalTickets = issuesByProject.Values.Sum(l => l.Count);
            tracker.UpdateCounts(jobId, "tickets", totalTickets, 0);
            await sink.NotifyAsync(tracker.GetProgress(jobId));

            var ticketMap = new Dictionary<string, Guid>();
            var ticketsMigrated = 0;
            var batchCount = 0;

            foreach (var (epProjectId, issues) in issuesByProject)
            {
                if (!projectMap.TryGetValue(epProjectId, out var spProjectId)) continue;

                foreach (var issue in issues)
                {
                    ct.ThrowIfCancellationRequested();
                    if (request.SkipClosedIssues && issue.StatusName?.ToLowerInvariant() is "closed" or "rejected")
                    {
                        tracker.IncrementSkipped(jobId);
                        continue;
                    }

                    try
                    {
                        var spTicketId = await MigrateTicket(jobId, issue, spProjectId, taskTypeMap, userMap, adminUserId, defaultStateId, defaultPriorityId, mergedStatusMapping, mergedPriorityMapping, ct);
                        ticketMap[issue.ExternalId] = spTicketId;
                        ticketsMigrated++;
                        batchCount++;
                        tracker.UpdateCounts(jobId, "tickets", totalTickets, ticketsMigrated);

                        if (batchCount >= 50)
                        {
                            await dbContext.SaveChangesAsync(ct);
                            batchCount = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        tracker.AddError(jobId, $"Failed to migrate ticket #{issue.ExternalId} '{issue.Title}': {ex.Message}");
                        logger.LogError(ex, "Failed to migrate ticket {ExternalId}", issue.ExternalId);
                    }
                }
            }
            await dbContext.SaveChangesAsync(ct);

            foreach (var issues in issuesByProject.Values)
            {
                foreach (var issue in issues.Where(i => i.ParentExternalId != null))
                {
                    if (ticketMap.TryGetValue(issue.ExternalId, out var childId) &&
                        ticketMap.TryGetValue(issue.ParentExternalId!, out var parentId))
                    {
                        var child = await dbContext.Tickets.FindAsync([childId], ct);
                        if (child != null) child.ParentTicketId = parentId;
                    }
                }
            }
            await dbContext.SaveChangesAsync(ct);
            await sink.AdvancePhaseAsync(MigrationPhase.Tickets, ct);

            if (request.ImportComments)
            {
                tracker.UpdatePhase(jobId, "Migrating comments");
                await sink.NotifyAsync(tracker.GetProgress(jobId));

                var totalComments = issuesByProject.Values.Sum(issues => issues.Sum(i => i.Comments.Count));
                var commentsMigrated = 0;
                tracker.UpdateCounts(jobId, "comments", totalComments, 0);

                foreach (var (epProjectId, issues) in issuesByProject)
                {
                    if (!projectMap.TryGetValue(epProjectId, out var spProjectId)) continue;

                    foreach (var issue in issues)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (!ticketMap.TryGetValue(issue.ExternalId, out var spTicketId)) continue;

                        foreach (var comment in issue.Comments)
                        {
                            try
                            {
                                await MigrateComment(jobId, comment, spTicketId, spProjectId, userMap, adminUserId, commentSource, ct);
                                commentsMigrated++;
                                tracker.UpdateCounts(jobId, "comments", totalComments, commentsMigrated);
                            }
                            catch (Exception ex)
                            {
                                tracker.AddError(jobId, $"Failed to migrate comment #{comment.ExternalId}: {ex.Message}");
                            }
                        }
                    }
                }
                await dbContext.SaveChangesAsync(ct);
            }
            await sink.AdvancePhaseAsync(MigrationPhase.Comments, ct);

            if (request.ImportWorklogs)
            {
                tracker.UpdatePhase(jobId, "Migrating worklogs");
                await sink.NotifyAsync(tracker.GetProgress(jobId));

                var totalWorklogs = worklogsByProject.Values.Sum(l => l.Count);
                var worklogsMigrated = 0;
                tracker.UpdateCounts(jobId, "worklogs", totalWorklogs, 0);

                foreach (var (epProjectId, worklogs) in worklogsByProject)
                {
                    if (!projectMap.TryGetValue(epProjectId, out _)) continue;

                    foreach (var worklog in worklogs)
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            await MigrateWorklog(jobId, worklog, ticketMap, userMap, adminUserId, systemName, ct);
                            worklogsMigrated++;
                            tracker.UpdateCounts(jobId, "worklogs", totalWorklogs, worklogsMigrated);
                        }
                        catch (Exception ex)
                        {
                            tracker.AddError(jobId, $"Failed to migrate time entry #{worklog.ExternalId}: {ex.Message}");
                        }
                    }
                }
                await dbContext.SaveChangesAsync(ct);
            }
            await sink.AdvancePhaseAsync(MigrationPhase.Worklogs, ct);

            tracker.UpdatePhase(jobId, "Migrating custom fields");
            await sink.NotifyAsync(tracker.GetProgress(jobId));

            foreach (var issues in issuesByProject.Values)
            {
                foreach (var issue in issues)
                {
                    if (!ticketMap.TryGetValue(issue.ExternalId, out var spTicketId)) continue;
                    if (issue.CustomFields.Count == 0) continue;
                    try { await MigrateCustomFields(issue.CustomFields, "Ticket", spTicketId, null, ct); }
                    catch (Exception ex) { tracker.AddError(jobId, $"Failed to migrate custom fields for ticket #{issue.ExternalId}: {ex.Message}"); }
                }
            }

            foreach (var project in selectedProjects)
            {
                if (!projectMap.TryGetValue(project.ExternalId, out var spProjectId)) continue;
                if (project.CustomFields.Count == 0) continue;
                try { await MigrateCustomFields(project.CustomFields, "Project", null, spProjectId, ct); }
                catch (Exception ex) { tracker.AddError(jobId, $"Failed to migrate custom fields for project '{project.Name}': {ex.Message}"); }
            }
            await dbContext.SaveChangesAsync(ct);
            await sink.AdvancePhaseAsync(MigrationPhase.CustomFields, ct);

            if (request.ImportChecklists)
            {
                tracker.UpdatePhase(jobId, "Migrating checklists");
                await sink.NotifyAsync(tracker.GetProgress(jobId));

                foreach (var issues in issuesByProject.Values)
                {
                    foreach (var issue in issues)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (!ticketMap.TryGetValue(issue.ExternalId, out var spTicketId)) continue;

                        foreach (var item in issue.ChecklistItems)
                        {
                            try { await MigrateChecklistItem(jobId, item, spTicketId, ct); }
                            catch (Exception ex) { tracker.AddError(jobId, $"Failed to migrate checklist item #{item.ExternalId}: {ex.Message}"); }
                        }
                    }
                }
                await dbContext.SaveChangesAsync(ct);
            }
            await sink.AdvancePhaseAsync(MigrationPhase.Checklists, ct);

            if (!request.SkipAttachments)
            {
                tracker.UpdatePhase(jobId, "Migrating attachments");
                await sink.NotifyAsync(tracker.GetProgress(jobId));

                var totalAttachments = issuesByProject.Values.Sum(issues => issues.Sum(i => i.Attachments.Count));
                var attachmentsMigrated = 0;
                tracker.UpdateCounts(jobId, "attachments", totalAttachments, 0);

                foreach (var issues in issuesByProject.Values)
                {
                    foreach (var issue in issues)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (!ticketMap.TryGetValue(issue.ExternalId, out var spTicketId)) continue;

                        foreach (var att in issue.Attachments)
                        {
                            try
                            {
                                await MigrateAttachment(jobId, att, spTicketId, connector, context, adminUserId, ct);
                                attachmentsMigrated++;
                                tracker.UpdateCounts(jobId, "attachments", totalAttachments, attachmentsMigrated);
                            }
                            catch (Exception ex) { tracker.AddError(jobId, $"Failed to migrate attachment '{att.FileName}': {ex.Message}"); }
                        }
                    }
                }
            }
            await sink.AdvancePhaseAsync(MigrationPhase.Attachments, ct);

            tracker.UpdatePhase(jobId, "Recalculating");
            await sink.NotifyAsync(tracker.GetProgress(jobId));

            foreach (var spProjectId in projectMap.Values)
            {
                // Roll worklog hours up the sub-ticket tree so CumulativeWorkedHours includes descendants.
                await CumulativeWorkedHoursCalculator.RecalculateProjectAsync(dbContext, spProjectId, ct);

                var project = await dbContext.Projects.FindAsync([spProjectId], ct);
                if (project != null)
                    project.SpentHours = await dbContext.Worklogs.Where(w => w.Ticket.ProjectId == spProjectId).SumAsync(w => w.Hours, ct);
            }
            await dbContext.SaveChangesAsync(ct);

            foreach (var (_, spId) in projectMap)
            {
                dbContext.SyncLogs.Add(new SyncLog
                {
                    Id = Guid.NewGuid(),
                    ProjectId = spId,
                    SyncType = connector.SourceSystem,
                    Status = SyncStatus.Success,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    ItemsSynced = ticketsMigrated,
                    ItemsFailed = 0
                });
            }

            var progress = tracker.GetProgress(jobId);
            var hasErrors = (progress?.ErrorCount ?? 0) > 0;
            tracker.Complete(jobId, hasErrors);
            await sink.CompleteAsync(hasErrors, progress, ct);

            tracker.AddLog(jobId, $"Migration completed. Projects: {projectsMigrated}, Tickets: {ticketsMigrated}");
            await sink.NotifyAsync(tracker.GetProgress(jobId));
        }
        catch (OperationCanceledException)
        {
            tracker.Cancel(jobId);
            tracker.AddLog(jobId, "Migration cancelled by user.");
            await sink.CancelAsync(CancellationToken.None);
            await sink.NotifyAsync(tracker.GetProgress(jobId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync job {JobId} failed", jobId);
            tracker.Fail(jobId, ex.Message);
            tracker.AddLog(jobId, $"Migration failed: {ex.Message}");
            await sink.FailAsync(ex.Message, CancellationToken.None);
            await sink.NotifyAsync(tracker.GetProgress(jobId));
        }
    }

    private async Task<Guid> MigrateProject(Guid jobId, CanonicalProject project, string baseUrl, Guid targetTemplateId, Guid adminUserId, string systemName, Guid? integrationConnectionId, Guid? targetCompanyId, CancellationToken ct)
    {
        var externalId = project.ExternalId;
        var existing = await dbContext.Projects.FirstOrDefaultAsync(p => p.ExternalSystem == systemName && p.ExternalProjectId == externalId, ct);
        if (existing != null)
        {
            existing.Name = project.Name; existing.Description = HtmlToMarkdown.Convert(project.DescriptionHtml); existing.Status = MapProjectStatus(project.Status);
            existing.StartDate = ParseDateOnly(project.StartDate); existing.DeadlineDate = ParseDateOnly(project.DueDate);
            if (integrationConnectionId is { } existingConnId) existing.IntegrationConnectionId = existingConnId;
            if (targetCompanyId is { } existingCompanyId) existing.CompanyId = existingCompanyId;
            tracker.IncrementUpdated(jobId); tracker.AddLog(jobId, $"Updated project '{project.Name}'"); return existing.Id;
        }

        var code = GenerateProjectCode(project.Name);
        code = await EnsureUniqueCode(code, ct);
        var spProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = project.Name,
            Code = code,
            Description = HtmlToMarkdown.Convert(project.DescriptionHtml),
            Status = MapProjectStatus(project.Status),
            ExternalSystem = systemName,
            ExternalProjectId = externalId,
            ExternalBaseUrl = baseUrl,
            StartDate = ParseDateOnly(project.StartDate),
            DeadlineDate = ParseDateOnly(project.DueDate),
            ProjectTemplateId = targetTemplateId,
            IntegrationConnectionId = integrationConnectionId,
            CompanyId = targetCompanyId,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Projects.Add(spProject);
        dbContext.ProjectMembers.Add(new ProjectMember { Id = Guid.NewGuid(), ProjectId = spProject.Id, UserId = adminUserId, Role = ProjectRole.ProjectManager, JoinedAt = DateTime.UtcNow });

        var board = new KanbanBoard { Id = Guid.NewGuid(), ProjectId = spProject.Id, Name = "Main Board", IsDefault = true, CreatedAt = DateTime.UtcNow };
        dbContext.KanbanBoards.Add(board);
        // Kanban columns only over the target template's states, not globally — otherwise
        // a KanbanColumn would point at states from other templates.
        var taskStates = await dbContext.TaskStates
            .Where(ts => ts.IsActive && ts.ProjectTemplateId == targetTemplateId)
            .OrderBy(ts => ts.SortOrder)
            .ToListAsync(ct);
        for (var i = 0; i < taskStates.Count; i++)
        {
            var col = new KanbanColumn { Id = Guid.NewGuid(), BoardId = board.Id, Name = taskStates[i].Name, Position = i, CreatedAt = DateTime.UtcNow };
            col.MapsToTaskStates.Add(taskStates[i]);
            dbContext.KanbanColumns.Add(col);
        }

        await dbContext.SaveChangesAsync(ct);
        tracker.IncrementCreated(jobId); tracker.AddLog(jobId, $"Created project '{project.Name}' ({code})"); return spProject.Id;
    }

    private async Task<Guid> MigrateTicket(Guid jobId, CanonicalIssue issue, Guid spProjectId,
        Dictionary<string, Guid> taskTypeMap, Dictionary<string, Guid> userMap, Guid adminUserId, Guid defaultStateId, Guid defaultPriorityId,
        Dictionary<string, Guid> statusMap, Dictionary<string, Guid> priorityMap, CancellationToken ct)
    {
        var externalId = issue.ExternalId;
        var existing = await dbContext.Tickets.FirstOrDefaultAsync(t => t.ProjectId == spProjectId && t.ExternalId == externalId, ct);
        var taskStateId = statusMap.GetValueOrDefault(issue.StatusExternalId ?? string.Empty, defaultStateId);
        var ticketPriorityId = priorityMap.GetValueOrDefault(issue.PriorityExternalId ?? string.Empty, defaultPriorityId);
        var taskTypeId = issue.TypeExternalId != null && taskTypeMap.TryGetValue(issue.TypeExternalId, out var ttId) ? ttId : (Guid?)null;
        var assigneeId = issue.Assignee != null && userMap.TryGetValue(issue.Assignee.ExternalId, out var aId) ? aId : (Guid?)null;
        var reporterId = issue.Reporter != null && userMap.TryGetValue(issue.Reporter.ExternalId, out var rId) ? rId : adminUserId;
        var column = await dbContext.KanbanColumns.Where(c => c.Board!.ProjectId == spProjectId && c.Board.IsDefault && c.MapsToTaskStates.Any(ts => ts.Id == taskStateId)).FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            existing.Title = issue.Title; existing.Description = HtmlToMarkdown.Convert(issue.DescriptionHtml); existing.TaskStateId = taskStateId; existing.TicketPriorityId = ticketPriorityId;
            existing.TaskTypeId = taskTypeId; existing.AssigneeId = assigneeId; existing.ReporterId = reporterId;
            existing.EstimatedHours = issue.EstimatedHours; existing.DueDate = ParseDateOnly(issue.DueDate); existing.ColumnId = column?.Id;
            existing.ExternalUrl = issue.WebUrl;
            existing.ExternalProject = issue.ProjectName;
            tracker.IncrementUpdated(jobId); return existing.Id;
        }

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = spProjectId,
            Title = issue.Title,
            Description = HtmlToMarkdown.Convert(issue.DescriptionHtml),
            TaskStateId = taskStateId,
            TicketPriorityId = ticketPriorityId,
            Position = 0,
            TaskTypeId = taskTypeId,
            AssigneeId = assigneeId,
            ReporterId = reporterId,
            ExternalId = externalId,
            ExternalUrl = issue.WebUrl,
            ExternalProject = issue.ProjectName,
            EstimatedHours = issue.EstimatedHours,
            DueDate = ParseDateOnly(issue.DueDate),
            ColumnId = column?.Id,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Tickets.Add(ticket); tracker.IncrementCreated(jobId); return ticket.Id;
    }

    private async Task MigrateComment(Guid jobId, CanonicalComment comment, Guid spTicketId, Guid spProjectId, Dictionary<string, Guid> userMap, Guid adminUserId, CommentSource source, CancellationToken ct)
    {
        var externalId = comment.ExternalId;
        if (await dbContext.Comments.AnyAsync(c => c.ExternalId == externalId && c.Source == source, ct)) { tracker.IncrementSkipped(jobId); return; }
        var authorId = comment.Author != null && userMap.TryGetValue(comment.Author.ExternalId, out var uid) ? uid : adminUserId;
        dbContext.Comments.Add(new Comment
        {
            Id = Guid.NewGuid(),
            TicketId = spTicketId,
            ProjectId = spProjectId,
            AuthorId = authorId,
            Content = HtmlToMarkdown.Convert(comment.BodyHtml) ?? string.Empty,
            IsInternal = comment.IsInternal,
            Source = source,
            ExternalId = externalId,
            ExternalUser = comment.Author?.DisplayName,
            CreatedAt = comment.CreatedAt ?? DateTime.UtcNow
        });
        tracker.IncrementCreated(jobId);
    }

    private async Task MigrateWorklog(Guid jobId, CanonicalWorklog worklog, Dictionary<string, Guid> ticketMap, Dictionary<string, Guid> userMap, Guid adminUserId, string systemName, CancellationToken ct)
    {
        var externalId = worklog.ExternalId;
        if (await dbContext.Worklogs.AnyAsync(w => w.ExternalId == externalId, ct)) { tracker.IncrementSkipped(jobId); return; }
        if (worklog.IssueExternalId == null || !ticketMap.TryGetValue(worklog.IssueExternalId, out var ticketId))
        {
            tracker.AddLog(jobId, $"Skipping worklog #{externalId} — no linked ticket in target project.");
            tracker.IncrementSkipped(jobId);
            return;
        }
        var userId = worklog.User != null && userMap.TryGetValue(worklog.User.ExternalId, out var uid) ? uid : adminUserId;
        var description = string.IsNullOrWhiteSpace(worklog.CommentHtml)
            ? $"Migrated from {systemName} time entry #{externalId}."
            : HtmlToMarkdown.Convert(worklog.CommentHtml) ?? worklog.CommentHtml!;
        dbContext.Worklogs.Add(new Worklog
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UserId = userId,
            Date = ParseDateOnly(worklog.SpentOn) ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Hours = worklog.Hours,
            Description = description,
            Source = WorklogSource.Sync,
            IsBillable = worklog.IsBillable,
            ExternalId = externalId,
            CreatedAt = DateTime.UtcNow
        });
        tracker.IncrementCreated(jobId);
    }

    private async Task MigrateCustomFields(IReadOnlyList<CanonicalCustomFieldValue> customFields, string appliesTo, Guid? spTicketId, Guid? spProjectId, CancellationToken ct)
    {
        foreach (var cf in customFields)
        {
            if (string.IsNullOrWhiteSpace(cf.Value)) continue;
            var definition = await GetOrCreateCustomFieldDefinition(cf, appliesTo, ct);
            if (spTicketId is { } ticketId)
            {
                var existing = await dbContext.TicketCustomFieldValues.FirstOrDefaultAsync(v => v.TicketId == ticketId && v.CustomFieldDefinitionId == definition.Id, ct);
                if (existing != null) existing.Value = cf.Value;
                else dbContext.TicketCustomFieldValues.Add(new TicketCustomFieldValue { Id = Guid.NewGuid(), TicketId = ticketId, CustomFieldDefinitionId = definition.Id, Value = cf.Value, CreatedAt = DateTime.UtcNow });
            }
            else if (spProjectId is { } projectId)
            {
                var existing = await dbContext.ProjectCustomFieldValues.FirstOrDefaultAsync(v => v.ProjectId == projectId && v.CustomFieldDefinitionId == definition.Id, ct);
                if (existing != null) existing.Value = cf.Value;
                else dbContext.ProjectCustomFieldValues.Add(new ProjectCustomFieldValue { Id = Guid.NewGuid(), ProjectId = projectId, CustomFieldDefinitionId = definition.Id, Value = cf.Value, CreatedAt = DateTime.UtcNow });
            }
        }
    }

    private async Task<CustomFieldDefinition> GetOrCreateCustomFieldDefinition(CanonicalCustomFieldValue cf, string appliesTo, CancellationToken ct)
    {
        var fieldType = MapCustomFieldType(cf.Format);
        var existing = await dbContext.CustomFieldDefinitions.FirstOrDefaultAsync(d => d.Name == cf.Name && d.AppliesTo == appliesTo, ct);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.Options) && fieldType == CustomFieldType.Select && cf.Options is { Count: > 0 })
                existing.Options = JsonSerializer.Serialize(cf.Options);
            return existing;
        }
        var definition = new CustomFieldDefinition { Id = Guid.NewGuid(), Name = cf.Name, FieldType = fieldType, AppliesTo = appliesTo, IsActive = true, CreatedAt = DateTime.UtcNow };
        if (fieldType == CustomFieldType.Select && cf.Options is { Count: > 0 })
            definition.Options = JsonSerializer.Serialize(cf.Options);
        dbContext.CustomFieldDefinitions.Add(definition); await dbContext.SaveChangesAsync(ct); return definition;
    }

    private async Task MigrateChecklistItem(Guid jobId, CanonicalChecklistItem item, Guid spTicketId, CancellationToken ct)
    {
        var externalId = item.ExternalId;
        var existing = await dbContext.ChecklistItems.FirstOrDefaultAsync(ci => ci.TicketId == spTicketId && ci.ExternalId == externalId, ct);
        if (existing != null) { if (existing.Text != item.Text || existing.IsCompleted != item.IsCompleted) { existing.Text = item.Text; existing.IsCompleted = item.IsCompleted; tracker.IncrementUpdated(jobId); } else tracker.IncrementSkipped(jobId); return; }
        dbContext.ChecklistItems.Add(new ChecklistItem { Id = Guid.NewGuid(), TicketId = spTicketId, Text = item.Text, IsCompleted = item.IsCompleted, Position = item.Position, ExternalId = externalId, CreatedAt = DateTime.UtcNow });
        tracker.IncrementCreated(jobId);
    }

    private async Task MigrateAttachment(Guid jobId, CanonicalAttachment att, Guid spTicketId, ISourceConnector connector, SourceConnectionContext context, Guid adminUserId, CancellationToken ct)
    {
        if (await dbContext.TicketAttachments.AnyAsync(a => a.TicketId == spTicketId && a.FileName == att.FileName && a.FileSizeBytes == att.FileSizeBytes, ct)) { tracker.IncrementSkipped(jobId); return; }
        await using var stream = await connector.DownloadAttachmentAsync(context, att.ContentUrl, ct);
        var blobName = $"migration/{spTicketId}/{Guid.NewGuid()}/{att.FileName}";
        var blobUrl = await blobStorage.UploadAsync("attachments", blobName, stream, att.ContentType ?? "application/octet-stream", ct);
        dbContext.TicketAttachments.Add(new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketId = spTicketId,
            FileName = att.FileName,
            BlobUrl = blobUrl,
            ContentType = att.ContentType ?? "application/octet-stream",
            FileSizeBytes = att.FileSizeBytes,
            UploadedById = adminUserId,
            CreatedAt = att.CreatedAt ?? DateTime.UtcNow
        });
        tracker.IncrementCreated(jobId); tracker.AddLog(jobId, $"Uploaded attachment '{att.FileName}'");
    }

    private async Task<Dictionary<string, Guid>> EnsureTaskTypes(Guid jobId, IReadOnlyDictionary<string, Guid?> trackerMapping, IReadOnlyDictionary<string, string>? autoCreate, CancellationToken ct)
    {
        var result = new Dictionary<string, Guid>();
        foreach (var (epTrackerId, spTaskTypeId) in trackerMapping)
        { if (spTaskTypeId.HasValue) result[epTrackerId] = spTaskTypeId.Value; else tracker.AddLog(jobId, $"Tracker #{epTrackerId} has no mapping, skipping"); }

        if (autoCreate != null)
        {
            foreach (var (epTrackerId, name) in autoCreate)
            {
                var existing = await dbContext.TaskTypes.FirstOrDefaultAsync(t => t.Name == name, ct);
                if (existing != null)
                {
                    result[epTrackerId] = existing.Id;
                    tracker.AddLog(jobId, $"Reusing existing TaskType '{name}' for tracker #{epTrackerId}");
                    continue;
                }

                var maxSort = await dbContext.TaskTypes.MaxAsync(t => (int?)t.SortOrder, ct) ?? 0;
                var taskType = new TaskType { Id = Guid.NewGuid(), Name = name, SortOrder = maxSort + 1, IsActive = true };
                dbContext.TaskTypes.Add(taskType);
                await dbContext.SaveChangesAsync(ct);
                result[epTrackerId] = taskType.Id;
                tracker.IncrementCreated(jobId);
                tracker.AddLog(jobId, $"Auto-created TaskType '{name}'");
            }
        }

        return result;
    }

    private async Task<Dictionary<string, Guid>> EnsureTaskStates(Guid jobId, Guid targetTemplateId, IReadOnlyDictionary<string, string>? autoCreate, IReadOnlyDictionary<string, bool>? isClosedMap, CancellationToken ct)
    {
        var result = new Dictionary<string, Guid>();
        if (autoCreate == null) return result;

        foreach (var (epStatusId, name) in autoCreate)
        {
            // Scope to the target template — otherwise state names would collide across
            // templates (e.g. two templates each with a "Done" state).
            var existing = await dbContext.TaskStates
                .FirstOrDefaultAsync(ts => ts.Name == name && ts.ProjectTemplateId == targetTemplateId, ct);
            if (existing != null)
            {
                result[epStatusId] = existing.Id;
                tracker.AddLog(jobId, $"Reusing existing TaskState '{name}' for status #{epStatusId}");
                continue;
            }

            var maxSort = await dbContext.TaskStates
                .Where(ts => ts.ProjectTemplateId == targetTemplateId)
                .MaxAsync(ts => (int?)ts.SortOrder, ct) ?? 0;
            var isClosed = isClosedMap?.GetValueOrDefault(epStatusId, false) ?? false;
            var taskState = new TaskState
            {
                Id = Guid.NewGuid(),
                Name = name,
                Color = "#6B7280",
                SortOrder = maxSort + 1,
                IsActive = true,
                IsDefault = false,
                IsClosedState = isClosed,
                ProjectTemplateId = targetTemplateId
            };
            dbContext.TaskStates.Add(taskState);
            await dbContext.SaveChangesAsync(ct);
            result[epStatusId] = taskState.Id;
            tracker.IncrementCreated(jobId);
            tracker.AddLog(jobId, $"Auto-created TaskState '{name}' (closed={isClosed})");
        }

        return result;
    }

    private async Task<Dictionary<string, Guid>> EnsureTicketPriorities(Guid jobId, Guid targetTemplateId, IReadOnlyDictionary<string, string>? autoCreate, CancellationToken ct)
    {
        var result = new Dictionary<string, Guid>();
        if (autoCreate == null) return result;

        foreach (var (epPriorityId, name) in autoCreate)
        {
            var existing = await dbContext.TicketPriorities
                .FirstOrDefaultAsync(tp => tp.Name == name && tp.ProjectTemplateId == targetTemplateId, ct);
            if (existing != null)
            {
                result[epPriorityId] = existing.Id;
                tracker.AddLog(jobId, $"Reusing existing TicketPriority '{name}' for priority #{epPriorityId}");
                continue;
            }

            var maxSort = await dbContext.TicketPriorities
                .Where(tp => tp.ProjectTemplateId == targetTemplateId)
                .MaxAsync(tp => (int?)tp.SortOrder, ct) ?? 0;
            var priority = new TicketPriority
            {
                Id = Guid.NewGuid(),
                Name = name,
                Color = "#6B7280",
                SortOrder = maxSort + 1,
                IsActive = true,
                IsDefault = false,
                ProjectTemplateId = targetTemplateId
            };
            dbContext.TicketPriorities.Add(priority);
            await dbContext.SaveChangesAsync(ct);
            result[epPriorityId] = priority.Id;
            tracker.IncrementCreated(jobId);
            tracker.AddLog(jobId, $"Auto-created TicketPriority '{name}'");
        }

        return result;
    }

    private async Task<Dictionary<string, Guid>> EnsureUsers(Guid jobId, SyncEngineRequest request, ISourceConnector connector, SourceConnectionContext context, string userPrefix, CancellationToken ct)
    {
        var result = new Dictionary<string, Guid>();
        IReadOnlyList<CanonicalUser>? sourceUsers = null;

        foreach (var (externalUserId, spUserId) in request.UserMapping)
        {
            if (spUserId.HasValue) { result[externalUserId] = spUserId.Value; continue; }
            if (!request.CreateMissingUsers) continue;

            sourceUsers ??= await connector.GetUsersAsync(context, ct);
            var sourceUser = sourceUsers.FirstOrDefault(u => u.ExternalId == externalUserId);
            if (sourceUser == null) continue;

            var displayName = $"{sourceUser.FirstName} {sourceUser.LastName}".Trim();
            if (string.IsNullOrEmpty(displayName)) displayName = sourceUser.Login ?? $"EP User {externalUserId}";
            var entraId = $"{userPrefix}:{externalUserId}";
            var existing = await dbContext.Users.FirstOrDefaultAsync(u => u.EntraObjectId == entraId, ct);
            if (existing != null) { result[externalUserId] = existing.Id; continue; }
            var user = new User
            {
                Id = Guid.NewGuid(),
                EntraObjectId = entraId,
                Email = sourceUser.Email ?? $"ep-{externalUserId}@migration.local",
                DisplayName = displayName,
                FirstName = sourceUser.FirstName,
                LastName = sourceUser.LastName,
                GlobalRole = GlobalRole.User,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.Users.Add(user); await dbContext.SaveChangesAsync(ct); result[externalUserId] = user.Id;
            tracker.IncrementCreated(jobId); tracker.AddLog(jobId, $"Created inactive user '{displayName}'");
        }
        return result;
    }


    private static CommentSource MapCommentSource(SyncType system) => system switch
    {
        SyncType.Jira => CommentSource.Jira,
        SyncType.Redmine => CommentSource.Redmine,
        SyncType.GitHub => CommentSource.GitHub,
        SyncType.Email => CommentSource.Email,
        _ => CommentSource.EasyProject
    };

    private static ProjectStatus MapProjectStatus(CanonicalProjectStatus status) => status switch
    {
        CanonicalProjectStatus.Completed => ProjectStatus.Completed,
        CanonicalProjectStatus.Archived => ProjectStatus.Archived,
        _ => ProjectStatus.Active
    };

    private static CustomFieldType MapCustomFieldType(CanonicalFieldFormat format) => format switch
    {
        CanonicalFieldFormat.Number => CustomFieldType.Number,
        CanonicalFieldFormat.Date => CustomFieldType.Date,
        CanonicalFieldFormat.Select => CustomFieldType.Select,
        _ => CustomFieldType.Text
    };

    private static DateOnly? ParseDateOnly(string? date) { if (string.IsNullOrWhiteSpace(date)) return null; return DateOnly.TryParse(date, out var d) ? d : null; }
    private static string GenerateProjectCode(string name) { var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries); if (words.Length == 1) return words[0][..Math.Min(3, words[0].Length)].ToUpperInvariant(); return string.Concat(words.Take(6).Select(w => char.ToUpperInvariant(w[0]))); }
    private async Task<string> EnsureUniqueCode(string baseCode, CancellationToken ct)
    {
        if (!await dbContext.Projects.AnyAsync(p => p.Code == baseCode, ct)) return baseCode;
        for (var i = 2; i <= 99; i++) { var candidate = $"{baseCode}{i}"; if (candidate.Length > 6) candidate = $"{baseCode[..Math.Max(2, 6 - i.ToString().Length)]}{i}"; if (!await dbContext.Projects.AnyAsync(p => p.Code == candidate, ct)) return candidate; }
        return $"{baseCode[..2]}{Guid.NewGuid().ToString()[..4].ToUpperInvariant()}";
    }
}

/// <summary>
/// Provider-agnostic sync configuration (external ids as strings). The EasyProject
/// adapter builds this from <c>StartMigrationCommand</c> by stringifying its int keys.
/// </summary>
public sealed record SyncEngineRequest(
    Guid TargetProjectTemplateId,
    IReadOnlyList<string> ProjectExternalIds,
    IReadOnlyDictionary<string, Guid?> TrackerMapping,
    IReadOnlyDictionary<string, Guid> StatusMapping,
    IReadOnlyDictionary<string, Guid> PriorityMapping,
    IReadOnlyDictionary<string, Guid?> UserMapping,
    bool SkipClosedIssues,
    bool SkipAttachments,
    bool ImportComments,
    bool ImportWorklogs,
    bool ImportChecklists,
    bool CreateMissingUsers,
    IReadOnlyDictionary<string, string>? AutoCreateTrackers,
    IReadOnlyDictionary<string, string>? AutoCreateStatuses,
    IReadOnlyDictionary<string, bool>? AutoCreateStatusIsClosed,
    IReadOnlyDictionary<string, string>? AutoCreatePriorities,
    // null = full pull (one-time import); non-null = only records changed at/after this
    // instant (incremental sync). Wired from a connection's watermark in milník 3c.
    DateTime? ChangedSince = null,
    // When set, created/updated projects are linked to this connection (Project.IntegrationConnectionId).
    Guid? IntegrationConnectionId = null,
    // When set, imported projects belong to this customer (Project.CompanyId).
    Guid? TargetCompanyId = null);
