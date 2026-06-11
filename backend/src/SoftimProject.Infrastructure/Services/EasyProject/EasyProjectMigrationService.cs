using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Migration.EasyProject;
using SoftimProject.Application.Features.Migration.EasyProject.Models;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Services.EasyProject;

public sealed class EasyProjectMigrationService(
    IApplicationDbContext dbContext,
    IEasyProjectApiClient apiClient,
    IMigrationProgressTracker tracker,
    IMigrationNotifier notifier,
    IBlobStorageService blobStorage,
    ILogger<EasyProjectMigrationService> logger) : IEasyProjectMigrationService
{
    public async Task ExecuteAsync(Guid jobId, StartMigrationCommand cmd)
    {
        try
        {
            var ct = tracker.GetCancellationToken(jobId);
            // Default TaskState/TicketPriority resolvujeme ze stavů cílové šablony,
            // ne globálně — ticket bez explicitního EP status musí spadnout do
            // stavu, který skutečně patří k jeho projektu (a tedy šabloně).
            var defaultStateId = await dbContext.TaskStates
                .Where(ts => ts.IsActive && ts.IsDefault && ts.ProjectTemplateId == cmd.TargetProjectTemplateId)
                .Select(ts => ts.Id)
                .FirstOrDefaultAsync(ct);
            if (defaultStateId == Guid.Empty)
                defaultStateId = await dbContext.TaskStates
                    .Where(ts => ts.IsActive && ts.ProjectTemplateId == cmd.TargetProjectTemplateId)
                    .OrderBy(ts => ts.SortOrder)
                    .Select(ts => ts.Id)
                    .FirstAsync(ct);

            var defaultPriorityId = await dbContext.TicketPriorities
                .Where(tp => tp.IsActive && tp.IsDefault && tp.ProjectTemplateId == cmd.TargetProjectTemplateId)
                .Select(tp => tp.Id)
                .FirstOrDefaultAsync(ct);
            if (defaultPriorityId == Guid.Empty)
                defaultPriorityId = await dbContext.TicketPriorities
                    .Where(tp => tp.IsActive && tp.ProjectTemplateId == cmd.TargetProjectTemplateId)
                    .OrderBy(tp => tp.SortOrder)
                    .Select(tp => tp.Id)
                    .FirstAsync(ct);

            // Phase 1: Fetch data from EP
            tracker.UpdatePhase(jobId, "Fetching data from EasyProject");
            tracker.AddLog(jobId, "Starting data fetch...");
            await NotifyProgress(jobId);

            var epProjects = new List<EpProject>();
            var epIssuesByProject = new Dictionary<int, List<EpIssue>>();
            var epTimeEntriesByProject = new Dictionary<int, List<EpTimeEntry>>();

            var allEpProjects = await apiClient.GetProjectsAsync(cmd.BaseUrl, cmd.ApiKey, ct);
            foreach (var epId in cmd.ProjectIds)
            {
                ct.ThrowIfCancellationRequested();
                var ep = allEpProjects.FirstOrDefault(p => p.Id == epId);
                if (ep == null) continue;
                epProjects.Add(ep);

                var issues = await apiClient.GetProjectIssuesAsync(cmd.BaseUrl, cmd.ApiKey, epId, ct);

                var detailedIssues = new List<EpIssue>();
                foreach (var issue in issues)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var detail = await apiClient.GetIssueDetailAsync(cmd.BaseUrl, cmd.ApiKey, issue.Id, ct);
                        detailedIssues.Add(detail);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to fetch issue detail #{IssueId}", issue.Id);
                        detailedIssues.Add(issue);
                        tracker.AddError(jobId, $"Failed to fetch issue #{issue.Id}: {ex.Message}");
                    }
                }
                epIssuesByProject[epId] = detailedIssues;

                if (cmd.ImportWorklogs)
                {
                    var timeEntries = await apiClient.GetProjectTimeEntriesAsync(cmd.BaseUrl, cmd.ApiKey, epId, ct);
                    epTimeEntriesByProject[epId] = timeEntries;
                }

                tracker.AddLog(jobId, $"Fetched project '{ep.Name}': {detailedIssues.Count} issues");
            }

            var epCustomFieldDefs = await apiClient.GetCustomFieldsAsync(cmd.BaseUrl, cmd.ApiKey, ct);
            var possibleValuesMap = epCustomFieldDefs
                .Where(d => d.PossibleValues is { Count: > 0 })
                .ToDictionary(d => d.Id, d => d.PossibleValues!);
            tracker.AddLog(jobId, $"Fetched {epCustomFieldDefs.Count} custom field definitions ({possibleValuesMap.Count} with possible values)");
            await AdvancePhaseAsync(jobId, MigrationPhase.Fetching, ct);

            // Phase 2: Lookups
            tracker.UpdatePhase(jobId, "Processing lookups");
            await NotifyProgress(jobId);

            var taskTypeMap = await EnsureTaskTypes(jobId, cmd.TrackerMapping, cmd.AutoCreateTrackers, ct);
            var taskStateMap = await EnsureTaskStates(jobId, cmd.TargetProjectTemplateId, cmd.AutoCreateStatuses, cmd.AutoCreateStatusIsClosed, ct);
            var priorityMap = await EnsureTicketPriorities(jobId, cmd.TargetProjectTemplateId, cmd.AutoCreatePriorities, ct);

            // Merge auto-created into existing mappings
            var mergedStatusMapping = new Dictionary<int, Guid>(cmd.StatusMapping);
            foreach (var (epId, spId) in taskStateMap)
                mergedStatusMapping[epId] = spId;

            var mergedPriorityMapping = new Dictionary<int, Guid>(cmd.PriorityMapping);
            foreach (var (epId, spId) in priorityMap)
                mergedPriorityMapping[epId] = spId;
            await AdvancePhaseAsync(jobId, MigrationPhase.Lookups, ct);

            // Phase 3: Users
            tracker.UpdatePhase(jobId, "Processing users");
            await NotifyProgress(jobId);

            var userMap = await EnsureUsers(jobId, cmd, ct);

            var adminUserId = await dbContext.MigrationJobs
                .Where(j => j.Id == jobId)
                .Select(j => j.InitiatedByUserId)
                .FirstAsync(ct);
            await AdvancePhaseAsync(jobId, MigrationPhase.Users, ct);

            // Phase 4: Projects
            tracker.UpdatePhase(jobId, "Migrating projects");
            tracker.UpdateCounts(jobId, "projects", epProjects.Count, 0);
            await NotifyProgress(jobId);

            var projectMap = new Dictionary<int, Guid>();
            var projectsMigrated = 0;

            foreach (var ep in epProjects)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var spProjectId = await MigrateProject(jobId, ep, cmd.BaseUrl, adminUserId, cmd.TargetProjectTemplateId, ct);
                    projectMap[ep.Id] = spProjectId;
                    projectsMigrated++;
                    tracker.UpdateCounts(jobId, "projects", epProjects.Count, projectsMigrated);
                }
                catch (Exception ex)
                {
                    tracker.AddError(jobId, $"Failed to migrate project '{ep.Name}': {ex.Message}");
                    logger.LogError(ex, "Failed to migrate project {EpProjectId}", ep.Id);
                }
            }

            foreach (var ep in epProjects.Where(p => p.Parent != null))
            {
                if (projectMap.TryGetValue(ep.Id, out var childId) &&
                    projectMap.TryGetValue(ep.Parent!.Id, out var parentId))
                {
                    var child = await dbContext.Projects.FindAsync([childId], ct);
                    if (child != null) child.ParentProjectId = parentId;
                }
            }
            await dbContext.SaveChangesAsync(ct);
            await AdvancePhaseAsync(jobId, MigrationPhase.Projects, ct);

            // Phase 5: Tickets
            tracker.UpdatePhase(jobId, "Migrating tickets");
            var totalTickets = epIssuesByProject.Values.Sum(l => l.Count);
            tracker.UpdateCounts(jobId, "tickets", totalTickets, 0);
            await NotifyProgress(jobId);

            var ticketMap = new Dictionary<int, Guid>();
            var ticketsMigrated = 0;
            var batchCount = 0;

            foreach (var (epProjectId, issues) in epIssuesByProject)
            {
                if (!projectMap.TryGetValue(epProjectId, out var spProjectId)) continue;

                foreach (var issue in issues)
                {
                    ct.ThrowIfCancellationRequested();
                    if (cmd.SkipClosedIssues && issue.Status?.Name?.ToLowerInvariant() is "closed" or "rejected")
                    {
                        tracker.IncrementSkipped(jobId);
                        continue;
                    }

                    try
                    {
                        var spTicketId = await MigrateTicket(jobId, issue, spProjectId, cmd, taskTypeMap, userMap, adminUserId, defaultStateId, defaultPriorityId, mergedStatusMapping, mergedPriorityMapping, ct);
                        ticketMap[issue.Id] = spTicketId;
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
                        tracker.AddError(jobId, $"Failed to migrate ticket #{issue.Id} '{issue.Subject}': {ex.Message}");
                        logger.LogError(ex, "Failed to migrate ticket {EpIssueId}", issue.Id);
                    }
                }
            }
            await dbContext.SaveChangesAsync(ct);

            foreach (var issues in epIssuesByProject.Values)
            {
                foreach (var issue in issues.Where(i => i.Parent != null))
                {
                    if (ticketMap.TryGetValue(issue.Id, out var childId) &&
                        ticketMap.TryGetValue(issue.Parent!.Id, out var parentId))
                    {
                        var child = await dbContext.Tickets.FindAsync([childId], ct);
                        if (child != null) child.ParentTicketId = parentId;
                    }
                }
            }
            await dbContext.SaveChangesAsync(ct);
            await AdvancePhaseAsync(jobId, MigrationPhase.Tickets, ct);

            if (cmd.ImportComments)
            {
                tracker.UpdatePhase(jobId, "Migrating comments");
                await NotifyProgress(jobId);

                var totalComments = 0;
                var commentsMigrated = 0;

                foreach (var issues in epIssuesByProject.Values)
                    foreach (var issue in issues)
                        totalComments += issue.Journals?.Count(j => !string.IsNullOrWhiteSpace(j.Notes)) ?? 0;

                tracker.UpdateCounts(jobId, "comments", totalComments, 0);

                foreach (var issues in epIssuesByProject.Values)
                {
                    foreach (var issue in issues)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (!ticketMap.TryGetValue(issue.Id, out var spTicketId)) continue;
                        var spProjectId = projectMap.GetValueOrDefault(issue.Project?.Id ?? 0);
                        if (spProjectId == Guid.Empty) continue;

                        foreach (var journal in issue.Journals ?? [])
                        {
                            if (string.IsNullOrWhiteSpace(journal.Notes)) continue;
                            try
                            {
                                await MigrateComment(jobId, journal, spTicketId, spProjectId, userMap, adminUserId, ct);
                                commentsMigrated++;
                                tracker.UpdateCounts(jobId, "comments", totalComments, commentsMigrated);
                            }
                            catch (Exception ex)
                            {
                                tracker.AddError(jobId, $"Failed to migrate comment #{journal.Id}: {ex.Message}");
                            }
                        }
                    }
                }
                await dbContext.SaveChangesAsync(ct);
            }
            await AdvancePhaseAsync(jobId, MigrationPhase.Comments, ct);

            if (cmd.ImportWorklogs)
            {
                tracker.UpdatePhase(jobId, "Migrating worklogs");
                await NotifyProgress(jobId);

                var totalWorklogs = epTimeEntriesByProject.Values.Sum(l => l.Count);
                var worklogsMigrated = 0;
                tracker.UpdateCounts(jobId, "worklogs", totalWorklogs, 0);

                foreach (var (epProjectId, timeEntries) in epTimeEntriesByProject)
                {
                    if (!projectMap.TryGetValue(epProjectId, out var spProjectId)) continue;

                    foreach (var te in timeEntries)
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            await MigrateWorklog(jobId, te, ticketMap, userMap, adminUserId, ct);
                            worklogsMigrated++;
                            tracker.UpdateCounts(jobId, "worklogs", totalWorklogs, worklogsMigrated);
                        }
                        catch (Exception ex)
                        {
                            tracker.AddError(jobId, $"Failed to migrate time entry #{te.Id}: {ex.Message}");
                        }
                    }
                }
                await dbContext.SaveChangesAsync(ct);
            }
            await AdvancePhaseAsync(jobId, MigrationPhase.Worklogs, ct);

            tracker.UpdatePhase(jobId, "Migrating custom fields");
            await NotifyProgress(jobId);

            foreach (var issues in epIssuesByProject.Values)
            {
                foreach (var issue in issues)
                {
                    if (!ticketMap.TryGetValue(issue.Id, out var spTicketId)) continue;
                    if (issue.CustomFields == null || issue.CustomFields.Count == 0) continue;
                    try { await MigrateTicketCustomFields(issue.CustomFields, spTicketId, possibleValuesMap, ct); }
                    catch (Exception ex) { tracker.AddError(jobId, $"Failed to migrate custom fields for ticket #{issue.Id}: {ex.Message}"); }
                }
            }

            foreach (var ep in epProjects)
            {
                if (!projectMap.TryGetValue(ep.Id, out var spProjectId)) continue;
                if (ep.CustomFields == null || ep.CustomFields.Count == 0) continue;
                try { await MigrateProjectCustomFields(ep.CustomFields, spProjectId, possibleValuesMap, ct); }
                catch (Exception ex) { tracker.AddError(jobId, $"Failed to migrate custom fields for project '{ep.Name}': {ex.Message}"); }
            }
            await dbContext.SaveChangesAsync(ct);
            await AdvancePhaseAsync(jobId, MigrationPhase.CustomFields, ct);

            if (cmd.ImportChecklists)
            {
                tracker.UpdatePhase(jobId, "Migrating checklists");
                await NotifyProgress(jobId);

                foreach (var issues in epIssuesByProject.Values)
                {
                    foreach (var issue in issues)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (!ticketMap.TryGetValue(issue.Id, out var spTicketId)) continue;
                        if (issue.EasyChecklists == null) continue;

                        foreach (var checklist in issue.EasyChecklists)
                        {
                            foreach (var item in checklist.Items ?? [])
                            {
                                try { await MigrateChecklistItem(jobId, item, spTicketId, ct); }
                                catch (Exception ex) { tracker.AddError(jobId, $"Failed to migrate checklist item #{item.Id}: {ex.Message}"); }
                            }
                        }
                    }
                }
                await dbContext.SaveChangesAsync(ct);
            }
            await AdvancePhaseAsync(jobId, MigrationPhase.Checklists, ct);

            if (!cmd.SkipAttachments)
            {
                tracker.UpdatePhase(jobId, "Migrating attachments");
                await NotifyProgress(jobId);

                var totalAttachments = 0;
                foreach (var issues in epIssuesByProject.Values)
                    foreach (var issue in issues)
                        totalAttachments += issue.Attachments?.Count ?? 0;

                var attachmentsMigrated = 0;
                tracker.UpdateCounts(jobId, "attachments", totalAttachments, 0);

                foreach (var issues in epIssuesByProject.Values)
                {
                    foreach (var issue in issues)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (!ticketMap.TryGetValue(issue.Id, out var spTicketId)) continue;
                        if (issue.Attachments == null) continue;

                        foreach (var att in issue.Attachments)
                        {
                            try
                            {
                                await MigrateAttachment(jobId, att, spTicketId, cmd.BaseUrl, cmd.ApiKey, adminUserId, ct);
                                attachmentsMigrated++;
                                tracker.UpdateCounts(jobId, "attachments", totalAttachments, attachmentsMigrated);
                            }
                            catch (Exception ex) { tracker.AddError(jobId, $"Failed to migrate attachment '{att.Filename}': {ex.Message}"); }
                        }
                    }
                }
            }

            await AdvancePhaseAsync(jobId, MigrationPhase.Attachments, ct);

            tracker.UpdatePhase(jobId, "Recalculating");
            await NotifyProgress(jobId);

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
                    SyncType = SyncType.EasyProject,
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

            var job = await dbContext.MigrationJobs.FindAsync([jobId], ct);
            if (job != null)
            {
                job.Status = hasErrors ? MigrationStatus.CompletedWithErrors : MigrationStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                job.CurrentPhase = MigrationPhase.Done;
                job.ProjectsMigrated = projectsMigrated;
                job.TicketsMigrated = ticketsMigrated;
                job.ItemsFailed = progress?.ErrorCount ?? 0;
                job.ItemsCreated = progress?.ItemsCreated ?? 0;
                job.ItemsUpdated = progress?.ItemsUpdated ?? 0;
                job.ItemsSkipped = progress?.ItemsSkipped ?? 0;
                job.ErrorLog = progress?.RecentErrors.Count > 0 ? JsonSerializer.Serialize(progress.RecentErrors) : null;
            }
            await dbContext.SaveChangesAsync(ct);

            tracker.AddLog(jobId, $"Migration completed. Projects: {projectsMigrated}, Tickets: {ticketsMigrated}");
            await NotifyProgress(jobId);
        }
        catch (OperationCanceledException)
        {
            tracker.Cancel(jobId);
            tracker.AddLog(jobId, "Migration cancelled by user.");
            var job = await dbContext.MigrationJobs.FindAsync([jobId], CancellationToken.None);
            if (job != null) { job.Status = MigrationStatus.Cancelled; job.CompletedAt = DateTime.UtcNow; }
            await dbContext.SaveChangesAsync(CancellationToken.None);
            await NotifyProgress(jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration job {JobId} failed", jobId);
            tracker.Fail(jobId, ex.Message);
            tracker.AddLog(jobId, $"Migration failed: {ex.Message}");
            var job = await dbContext.MigrationJobs.FindAsync([jobId], CancellationToken.None);
            if (job != null) { job.Status = MigrationStatus.Failed; job.CompletedAt = DateTime.UtcNow; job.ErrorLog = JsonSerializer.Serialize(new[] { ex.Message }); }
            await dbContext.SaveChangesAsync(CancellationToken.None);
            await NotifyProgress(jobId);
        }
    }

    // Persists the current phase so /admin/migration can show "last good boundary" even
    // if the process crashes, and so ResumeMigrationCommand reports progress correctly.
    // Advance is at phase boundaries, not mid-batch — recovery is phase-granular.
    private async Task AdvancePhaseAsync(Guid jobId, MigrationPhase phase, CancellationToken ct)
    {
        var job = await dbContext.MigrationJobs.FindAsync([jobId], ct);
        if (job is null) return;
        if (phase > job.CurrentPhase)
        {
            job.CurrentPhase = phase;
            await dbContext.SaveChangesAsync(ct);
        }
    }

    private async Task<Guid> MigrateProject(Guid jobId, EpProject ep, string baseUrl, Guid adminUserId, Guid targetTemplateId, CancellationToken ct)
    {
        var externalId = ep.Id.ToString();
        var existing = await dbContext.Projects.FirstOrDefaultAsync(p => p.ExternalSystem == "EasyProject" && p.ExternalProjectId == externalId, ct);
        if (existing != null)
        {
            existing.Name = ep.Name; existing.Description = ep.Description; existing.Status = MapProjectStatus(ep.Status);
            existing.StartDate = ParseDateOnly(ep.StartDate); existing.DeadlineDate = ParseDateOnly(ep.DueDate);
            tracker.IncrementUpdated(jobId); tracker.AddLog(jobId, $"Updated project '{ep.Name}'"); return existing.Id;
        }

        var code = GenerateProjectCode(ep.Name);
        code = await EnsureUniqueCode(code, ct);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = ep.Name,
            Code = code,
            Description = ep.Description,
            Status = MapProjectStatus(ep.Status),
            ExternalSystem = "EasyProject",
            ExternalProjectId = externalId,
            ExternalBaseUrl = baseUrl,
            StartDate = ParseDateOnly(ep.StartDate),
            DeadlineDate = ParseDateOnly(ep.DueDate),
            ProjectTemplateId = targetTemplateId,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Projects.Add(project);
        dbContext.ProjectMembers.Add(new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, UserId = adminUserId, Role = ProjectRole.ProjectManager, JoinedAt = DateTime.UtcNow });

        var board = new KanbanBoard { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "Main Board", IsDefault = true, CreatedAt = DateTime.UtcNow };
        dbContext.KanbanBoards.Add(board);
        // Kanban sloupce zakládáme pouze nad stavy cílové šablony, ne globálně —
        // jinak by KanbanColumn ukazoval na stavy jiných šablon.
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
        tracker.IncrementCreated(jobId); tracker.AddLog(jobId, $"Created project '{ep.Name}' ({code})"); return project.Id;
    }

    private async Task<Guid> MigrateTicket(Guid jobId, EpIssue issue, Guid spProjectId, StartMigrationCommand cmd,
        Dictionary<int, Guid> taskTypeMap, Dictionary<int, Guid> userMap, Guid adminUserId, Guid defaultStateId, Guid defaultPriorityId,
        Dictionary<int, Guid> statusMap, Dictionary<int, Guid> priorityMap, CancellationToken ct)
    {
        var externalId = issue.Id.ToString();
        var existing = await dbContext.Tickets.FirstOrDefaultAsync(t => t.ProjectId == spProjectId && t.ExternalId == externalId, ct);
        var taskStateId = statusMap.GetValueOrDefault(issue.Status?.Id ?? 0, defaultStateId);
        var ticketPriorityId = priorityMap.GetValueOrDefault(issue.Priority?.Id ?? 0, defaultPriorityId);
        var taskTypeId = issue.Tracker != null && taskTypeMap.TryGetValue(issue.Tracker.Id, out var ttId) ? ttId : (Guid?)null;
        var assigneeId = issue.AssignedTo != null && userMap.TryGetValue(issue.AssignedTo.Id, out var aId) ? aId : (Guid?)null;
        var reporterId = issue.Author != null && userMap.TryGetValue(issue.Author.Id, out var rId) ? rId : adminUserId;
        var column = await dbContext.KanbanColumns.Where(c => c.Board!.ProjectId == spProjectId && c.Board.IsDefault && c.MapsToTaskStates.Any(ts => ts.Id == taskStateId)).FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            existing.Title = issue.Subject; existing.Description = issue.Description; existing.TaskStateId = taskStateId; existing.TicketPriorityId = ticketPriorityId;
            existing.TaskTypeId = taskTypeId; existing.AssigneeId = assigneeId; existing.ReporterId = reporterId;
            existing.EstimatedHours = issue.EstimatedHours; existing.DueDate = ParseDateOnly(issue.DueDate); existing.ColumnId = column?.Id;
            existing.ExternalUrl = $"{cmd.BaseUrl.TrimEnd('/')}/issues/{issue.Id}";
            tracker.IncrementUpdated(jobId); return existing.Id;
        }

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = spProjectId,
            Title = issue.Subject,
            Description = issue.Description,
            TaskStateId = taskStateId,
            TicketPriorityId = ticketPriorityId,
            Position = 0,
            TaskTypeId = taskTypeId,
            AssigneeId = assigneeId,
            ReporterId = reporterId,
            ExternalId = externalId,
            ExternalUrl = $"{cmd.BaseUrl.TrimEnd('/')}/issues/{issue.Id}",
            EstimatedHours = issue.EstimatedHours,
            DueDate = ParseDateOnly(issue.DueDate),
            ColumnId = column?.Id,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Tickets.Add(ticket); tracker.IncrementCreated(jobId); return ticket.Id;
    }

    private async Task MigrateComment(Guid jobId, EpJournal journal, Guid spTicketId, Guid spProjectId, Dictionary<int, Guid> userMap, Guid adminUserId, CancellationToken ct)
    {
        var externalId = journal.Id.ToString();
        if (await dbContext.Comments.AnyAsync(c => c.ExternalId == externalId && c.Source == CommentSource.EasyProject, ct)) { tracker.IncrementSkipped(jobId); return; }
        var authorId = journal.User != null && userMap.TryGetValue(journal.User.Id, out var uid) ? uid : adminUserId;
        dbContext.Comments.Add(new Comment
        {
            Id = Guid.NewGuid(),
            TicketId = spTicketId,
            ProjectId = spProjectId,
            AuthorId = authorId,
            Content = journal.Notes ?? string.Empty,
            IsInternal = journal.PrivateNotes,
            Source = CommentSource.EasyProject,
            ExternalId = externalId,
            ExternalUser = journal.User?.Name,
            CreatedAt = ParseDateTime(journal.CreatedOn) ?? DateTime.UtcNow
        });
        tracker.IncrementCreated(jobId);
    }

    private async Task MigrateWorklog(Guid jobId, EpTimeEntry te, Dictionary<int, Guid> ticketMap, Dictionary<int, Guid> userMap, Guid adminUserId, CancellationToken ct)
    {
        var externalId = te.Id.ToString();
        if (await dbContext.Worklogs.AnyAsync(w => w.ExternalId == externalId, ct)) { tracker.IncrementSkipped(jobId); return; }
        if (te.Issue == null || !ticketMap.TryGetValue(te.Issue.Id, out var ticketId))
        {
            tracker.AddLog(jobId, $"Skipping worklog #{externalId} — no linked ticket in target project.");
            tracker.IncrementSkipped(jobId);
            return;
        }
        var userId = te.User != null && userMap.TryGetValue(te.User.Id, out var uid) ? uid : adminUserId;
        var description = string.IsNullOrWhiteSpace(te.Comments)
            ? $"Migrated from EasyProject time entry #{externalId}."
            : te.Comments!;
        dbContext.Worklogs.Add(new Worklog
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UserId = userId,
            Date = ParseDateOnly(te.SpentOn) ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Hours = te.Hours,
            Description = description,
            Source = WorklogSource.Sync,
            IsBillable = te.EasyIsBillable ?? false,
            ExternalId = externalId,
            CreatedAt = DateTime.UtcNow
        });
        tracker.IncrementCreated(jobId);
    }

    private async Task MigrateTicketCustomFields(List<EpCustomField> customFields, Guid spTicketId, Dictionary<int, List<EpPossibleValue>> possibleValuesMap, CancellationToken ct)
    {
        foreach (var cf in customFields)
        {
            var valueStr = cf.Value?.ToString(); if (string.IsNullOrWhiteSpace(valueStr)) continue;
            var definition = await GetOrCreateCustomFieldDefinition(cf, "Ticket", possibleValuesMap, ct);
            var existing = await dbContext.TicketCustomFieldValues.FirstOrDefaultAsync(v => v.TicketId == spTicketId && v.CustomFieldDefinitionId == definition.Id, ct);
            if (existing != null) existing.Value = valueStr;
            else dbContext.TicketCustomFieldValues.Add(new TicketCustomFieldValue { Id = Guid.NewGuid(), TicketId = spTicketId, CustomFieldDefinitionId = definition.Id, Value = valueStr, CreatedAt = DateTime.UtcNow });
        }
    }

    private async Task MigrateProjectCustomFields(List<EpCustomField> customFields, Guid spProjectId, Dictionary<int, List<EpPossibleValue>> possibleValuesMap, CancellationToken ct)
    {
        foreach (var cf in customFields)
        {
            var valueStr = cf.Value?.ToString(); if (string.IsNullOrWhiteSpace(valueStr)) continue;
            var definition = await GetOrCreateCustomFieldDefinition(cf, "Project", possibleValuesMap, ct);
            var existing = await dbContext.ProjectCustomFieldValues.FirstOrDefaultAsync(v => v.ProjectId == spProjectId && v.CustomFieldDefinitionId == definition.Id, ct);
            if (existing != null) existing.Value = valueStr;
            else dbContext.ProjectCustomFieldValues.Add(new ProjectCustomFieldValue { Id = Guid.NewGuid(), ProjectId = spProjectId, CustomFieldDefinitionId = definition.Id, Value = valueStr, CreatedAt = DateTime.UtcNow });
        }
    }

    private async Task<CustomFieldDefinition> GetOrCreateCustomFieldDefinition(EpCustomField cf, string appliesTo, Dictionary<int, List<EpPossibleValue>> possibleValuesMap, CancellationToken ct)
    {
        var existing = await dbContext.CustomFieldDefinitions.FirstOrDefaultAsync(d => d.Name == cf.Name && d.AppliesTo == appliesTo, ct);
        if (existing != null)
        {
            if (string.IsNullOrEmpty(existing.Options) && MapFieldFormat(cf.FieldFormat) == CustomFieldType.Select
                && possibleValuesMap.TryGetValue(cf.Id, out var pvs) && pvs.Count > 0)
            {
                existing.Options = JsonSerializer.Serialize(
                    pvs.Select(p => p.Value ?? p.Label ?? "").Where(v => v != "").Distinct().ToList());
            }
            return existing;
        }
        var definition = new CustomFieldDefinition { Id = Guid.NewGuid(), Name = cf.Name, FieldType = MapFieldFormat(cf.FieldFormat), AppliesTo = appliesTo, IsActive = true, CreatedAt = DateTime.UtcNow };
        if (MapFieldFormat(cf.FieldFormat) == CustomFieldType.Select
            && possibleValuesMap.TryGetValue(cf.Id, out var possibleValues) && possibleValues.Count > 0)
        {
            definition.Options = JsonSerializer.Serialize(
                possibleValues.Select(p => p.Value ?? p.Label ?? "").Where(v => v != "").Distinct().ToList());
        }
        dbContext.CustomFieldDefinitions.Add(definition); await dbContext.SaveChangesAsync(ct); return definition;
    }

    private async Task MigrateChecklistItem(Guid jobId, EpChecklistItem item, Guid spTicketId, CancellationToken ct)
    {
        var externalId = item.Id.ToString();
        var existing = await dbContext.ChecklistItems.FirstOrDefaultAsync(ci => ci.TicketId == spTicketId && ci.ExternalId == externalId, ct);
        if (existing != null) { if (existing.Text != item.Subject || existing.IsCompleted != item.Done) { existing.Text = item.Subject; existing.IsCompleted = item.Done; tracker.IncrementUpdated(jobId); } else tracker.IncrementSkipped(jobId); return; }
        dbContext.ChecklistItems.Add(new ChecklistItem { Id = Guid.NewGuid(), TicketId = spTicketId, Text = item.Subject, IsCompleted = item.Done, Position = item.Position, ExternalId = externalId, CreatedAt = DateTime.UtcNow });
        tracker.IncrementCreated(jobId);
    }

    private async Task MigrateAttachment(Guid jobId, EpAttachment att, Guid spTicketId, string baseUrl, string apiKey, Guid adminUserId, CancellationToken ct)
    {
        if (await dbContext.TicketAttachments.AnyAsync(a => a.TicketId == spTicketId && a.FileName == att.Filename && a.FileSizeBytes == att.Filesize, ct)) { tracker.IncrementSkipped(jobId); return; }
        await using var stream = await apiClient.DownloadAttachmentAsync(baseUrl, apiKey, att.ContentUrl, ct);
        var blobName = $"migration/{spTicketId}/{Guid.NewGuid()}/{att.Filename}";
        var blobUrl = await blobStorage.UploadAsync("attachments", blobName, stream, att.ContentType ?? "application/octet-stream", ct);
        dbContext.TicketAttachments.Add(new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketId = spTicketId,
            FileName = att.Filename,
            BlobUrl = blobUrl,
            ContentType = att.ContentType ?? "application/octet-stream",
            FileSizeBytes = att.Filesize,
            UploadedById = adminUserId,
            CreatedAt = ParseDateTime(att.CreatedOn) ?? DateTime.UtcNow
        });
        tracker.IncrementCreated(jobId); tracker.AddLog(jobId, $"Uploaded attachment '{att.Filename}'");
    }

    private async Task<Dictionary<int, Guid>> EnsureTaskTypes(Guid jobId, Dictionary<int, Guid?> trackerMapping, Dictionary<int, string>? autoCreate, CancellationToken ct)
    {
        var result = new Dictionary<int, Guid>();
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

    private async Task<Dictionary<int, Guid>> EnsureTaskStates(Guid jobId, Guid targetTemplateId, Dictionary<int, string>? autoCreate, Dictionary<int, bool>? isClosedMap, CancellationToken ct)
    {
        var result = new Dictionary<int, Guid>();
        if (autoCreate == null) return result;

        foreach (var (epStatusId, name) in autoCreate)
        {
            // Scope na cílovou šablonu — bez toho by se křížila jména stavů
            // mezi šablonami (např. dvě šablony se stavem "Done").
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

    private async Task<Dictionary<int, Guid>> EnsureTicketPriorities(Guid jobId, Guid targetTemplateId, Dictionary<int, string>? autoCreate, CancellationToken ct)
    {
        var result = new Dictionary<int, Guid>();
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

    private async Task<Dictionary<int, Guid>> EnsureUsers(Guid jobId, StartMigrationCommand cmd, CancellationToken ct)
    {
        var result = new Dictionary<int, Guid>();
        foreach (var (epUserId, spUserId) in cmd.UserMapping)
        {
            if (spUserId.HasValue) { result[epUserId] = spUserId.Value; continue; }
            if (!cmd.CreateMissingUsers) continue;
            var epUsers = await apiClient.GetUsersAsync(cmd.BaseUrl, cmd.ApiKey, ct);
            var epUser = epUsers.FirstOrDefault(u => u.Id == epUserId); if (epUser == null) continue;
            var displayName = $"{epUser.Firstname} {epUser.Lastname}".Trim();
            if (string.IsNullOrEmpty(displayName)) displayName = epUser.Login ?? $"EP User {epUserId}";
            var entraId = $"easyproject:{epUserId}";
            var existing = await dbContext.Users.FirstOrDefaultAsync(u => u.EntraObjectId == entraId, ct);
            if (existing != null) { result[epUserId] = existing.Id; continue; }
            var user = new User
            {
                Id = Guid.NewGuid(),
                EntraObjectId = entraId,
                Email = epUser.Mail ?? $"ep-{epUserId}@migration.local",
                DisplayName = displayName,
                FirstName = epUser.Firstname,
                LastName = epUser.Lastname,
                GlobalRole = GlobalRole.User,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.Users.Add(user); await dbContext.SaveChangesAsync(ct); result[epUserId] = user.Id;
            tracker.IncrementCreated(jobId); tracker.AddLog(jobId, $"Created inactive user '{displayName}'");
        }
        return result;
    }

    private async Task NotifyProgress(Guid jobId) { var progress = tracker.GetProgress(jobId); if (progress != null) await notifier.NotifyProgressAsync(jobId, progress); }
    private static ProjectStatus MapProjectStatus(int epStatus) => epStatus switch { 5 => ProjectStatus.Completed, 9 => ProjectStatus.Archived, _ => ProjectStatus.Active };
    private static CustomFieldType MapFieldFormat(string? fieldFormat) => fieldFormat?.ToLowerInvariant() switch { "int" or "float" => CustomFieldType.Number, "date" => CustomFieldType.Date, "list" or "enumeration" => CustomFieldType.Select, _ => CustomFieldType.Text };
    private static DateOnly? ParseDateOnly(string? date) { if (string.IsNullOrWhiteSpace(date)) return null; return DateOnly.TryParse(date, out var d) ? d : null; }
    private static DateTime? ParseDateTime(string? dt) { if (string.IsNullOrWhiteSpace(dt)) return null; return DateTime.TryParse(dt, out var d) ? d.ToUniversalTime() : null; }
    private static string GenerateProjectCode(string name) { var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries); if (words.Length == 1) return words[0][..Math.Min(3, words[0].Length)].ToUpperInvariant(); return string.Concat(words.Take(6).Select(w => char.ToUpperInvariant(w[0]))); }
    private async Task<string> EnsureUniqueCode(string baseCode, CancellationToken ct)
    {
        if (!await dbContext.Projects.AnyAsync(p => p.Code == baseCode, ct)) return baseCode;
        for (var i = 2; i <= 99; i++) { var candidate = $"{baseCode}{i}"; if (candidate.Length > 6) candidate = $"{baseCode[..Math.Max(2, 6 - i.ToString().Length)]}{i}"; if (!await dbContext.Projects.AnyAsync(p => p.Code == candidate, ct)) return candidate; }
        return $"{baseCode[..2]}{Guid.NewGuid().ToString()[..4].ToUpperInvariant()}";
    }
}
