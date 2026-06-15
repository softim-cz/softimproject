using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Octokit;
using SoftimProject.Application.Features.Projects.GitHub;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;
using DomainProject = SoftimProject.Domain.Entities.Project;
using DomainTicket = SoftimProject.Domain.Entities.Ticket;
using DomainComment = SoftimProject.Domain.Entities.Comment;

namespace SoftimProject.Infrastructure.Services;

public static class GitHubSyncHelper
{
    public static async Task<(int Synced, int Failed)> SyncAsync(
        IGitHubClient client,
        string owner,
        string repo,
        DomainProject project,
        IApplicationDbContext db,
        DateTime? lastSync,
        Guid defaultStateId,
        Guid closedStateId,
        Guid defaultPriorityId,
        ILogger logger,
        CancellationToken ct)
    {
        var synced = 0;
        var failed = 0;

        // --- Outbound: Push new tickets to GitHub ---
        var outboundTickets = await db.Tickets
            .Include(t => t.TaskState)
            .Where(t => t.ProjectId == project.Id && t.ExternalId == null)
            .ToListAsync(ct);

        foreach (var ticket in outboundTickets)
        {
            try
            {
                var newIssue = new NewIssue(ticket.Title)
                {
                    Body = ticket.Description ?? ""
                };

                var created = await client.Issue.Create(owner, repo, newIssue);
                ticket.ExternalId = created.Number.ToString();
                ticket.ExternalUrl = created.HtmlUrl;
                synced++;

                if (ticket.TaskState.IsClosedState)
                {
                    await client.Issue.Update(owner, repo, created.Number, new IssueUpdate { State = ItemState.Closed });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to push ticket {TicketId} to GitHub", ticket.Id);
                failed++;
            }
        }

        // --- Outbound: Push comments ---
        var ticketsWithExternalId = await db.Tickets
            .Where(t => t.ProjectId == project.Id && t.ExternalId != null)
            .Select(t => new { t.Id, t.ExternalId })
            .ToListAsync(ct);

        var ticketIds = ticketsWithExternalId.Select(t => t.Id).ToList();

        var outboundComments = await db.Comments
            .Where(c => c.TicketId != null && ticketIds.Contains(c.TicketId!.Value) && c.Source != CommentSource.GitHub && c.ExternalId == null)
            .ToListAsync(ct);

        foreach (var comment in outboundComments)
        {
            try
            {
                var ticketExtId = ticketsWithExternalId.FirstOrDefault(t => t.Id == comment.TicketId)?.ExternalId;
                if (ticketExtId == null || !int.TryParse(ticketExtId, out var issueNumber)) continue;

                var created = await client.Issue.Comment.Create(owner, repo, issueNumber, comment.Content);
                comment.ExternalId = created.Id.ToString();
                synced++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to push comment {CommentId} to GitHub", comment.Id);
                failed++;
            }
        }

        // --- Outbound: Push lifecycle changes of linked tickets to their issues ---
        // Only tickets changed since the last successful sync, to avoid needless writes
        // and ping-pong loops (the inbound pull / webhook are equality-guarded).
        var changedLinkedTickets = await db.Tickets
            .Include(t => t.TaskState)
            .Include(t => t.Assignee)
            .Include(t => t.TicketPriority)
            .Include(t => t.TaskType)
            .Where(t => t.ProjectId == project.Id
                && t.ExternalId != null
                && (lastSync == null || t.UpdatedAt > lastSync))
            .ToListAsync(ct);

        foreach (var ticket in changedLinkedTickets)
        {
            if (!int.TryParse(ticket.ExternalId, out var issueNumber)) continue;
            try
            {
                var update = new IssueUpdate
                {
                    Title = ticket.Title,
                    Body = ticket.Description ?? "",
                    State = ticket.TaskState.IsClosedState ? ItemState.Closed : ItemState.Open,
                };

                // --- Metadata mapping (#110): push assignee + priority/type as labels ---
                var assigneeLogin = ticket.Assignee?.GitHubLogin;
                if (!string.IsNullOrWhiteSpace(assigneeLogin))
                    update.AddAssignee(assigneeLogin);

                var priorityLabel = ticket.TicketPriority?.Name;
                if (!string.IsNullOrWhiteSpace(priorityLabel))
                    update.AddLabel(priorityLabel);
                var typeLabel = ticket.TaskType?.Name;
                if (!string.IsNullOrWhiteSpace(typeLabel))
                    update.AddLabel(typeLabel);

                await client.Issue.Update(owner, repo, issueNumber, update);
                synced++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to push ticket {TicketId} lifecycle to GitHub issue #{IssueNumber}", ticket.Id, issueNumber);
                failed++;
            }
        }

        // --- Inbound: Pull issues from GitHub ---
        var request = new RepositoryIssueRequest
        {
            State = ItemStateFilter.All,
            SortProperty = IssueSort.Updated,
            SortDirection = SortDirection.Descending
        };
        if (lastSync.HasValue)
            request.Since = new DateTimeOffset(lastSync.Value, TimeSpan.Zero);

        var issues = await client.Issue.GetAllForRepository(owner, repo, request);

        var fallbackAuthorId = await db.ProjectMembers
            .Where(pm => pm.ProjectId == project.Id)
            .OrderBy(pm => pm.Id)
            .Select(pm => pm.UserId)
            .FirstOrDefaultAsync(ct);

        if (fallbackAuthorId == Guid.Empty)
            fallbackAuthorId = await db.Users.Select(u => u.Id).FirstOrDefaultAsync(ct);

        foreach (var issue in issues)
        {
            if (issue.PullRequest != null) continue;

            try
            {
                var externalId = issue.Number.ToString();

                // --- Metadata mapping (#110): label → priority/type, assignee → řešitel ---
                var labelNames = issue.Labels.Select(l => l.Name).ToList();
                var assigneeLogins = issue.Assignees.Select(a => a.Login).ToList();
                var mappedAssignee = await GitHubMetadataMapper.ResolveAssigneeIdAsync(db, assigneeLogins, ct);
                var mappedPriority = await GitHubMetadataMapper.ResolvePriorityIdAsync(db, project.ProjectTemplateId, labelNames, ct);
                var mappedType = await GitHubMetadataMapper.ResolveTaskTypeIdAsync(db, labelNames, ct);

                var existingTicket = await db.Tickets
                    .Include(t => t.TaskState)
                    .FirstOrDefaultAsync(t => t.ProjectId == project.Id && t.ExternalId == externalId, ct);

                if (existingTicket != null)
                {
                    // Equality guard: only mutate (and bump UpdatedAt) when values actually
                    // differ, so the outbound push below doesn't re-fire every sync cycle.
                    var changed = false;
                    if (existingTicket.Title != issue.Title) { existingTicket.Title = issue.Title; changed = true; }
                    if (existingTicket.Description != issue.Body) { existingTicket.Description = issue.Body; changed = true; }
                    if (existingTicket.ExternalUrl != issue.HtmlUrl) { existingTicket.ExternalUrl = issue.HtmlUrl; changed = true; }

                    if (mappedAssignee != null && existingTicket.AssigneeId != mappedAssignee)
                    { existingTicket.AssigneeId = mappedAssignee; changed = true; }
                    if (mappedPriority != null && existingTicket.TicketPriorityId != mappedPriority.Value)
                    { existingTicket.TicketPriorityId = mappedPriority.Value; changed = true; }
                    if (mappedType != null && existingTicket.TaskTypeId != mappedType)
                    { existingTicket.TaskTypeId = mappedType; changed = true; }

                    if (issue.State.Value == ItemState.Closed && !existingTicket.TaskState.IsClosedState)
                    { existingTicket.TaskStateId = closedStateId; changed = true; }
                    else if (issue.State.Value == ItemState.Open && existingTicket.TaskState.IsClosedState)
                    { existingTicket.TaskStateId = defaultStateId; changed = true; }

                    if (changed)
                        existingTicket.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newTicket = new DomainTicket
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = project.Id,
                        Title = issue.Title,
                        Description = issue.Body,
                        TicketPriorityId = mappedPriority ?? defaultPriorityId,
                        TaskTypeId = mappedType,
                        AssigneeId = mappedAssignee,
                        TaskStateId = issue.State.Value == ItemState.Closed ? closedStateId : defaultStateId,
                        ReporterId = fallbackAuthorId,
                        ExternalId = externalId,
                        ExternalUrl = issue.HtmlUrl,
                        ExternalUser = issue.User?.Login,
                        Position = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.Tickets.Add(newTicket);
                }
                synced++;

                var issueComments = await client.Issue.Comment.GetAllForIssue(owner, repo, issue.Number);
                foreach (var ghComment in issueComments)
                {
                    var commentExtId = ghComment.Id.ToString();
                    var exists = await db.Comments
                        .AnyAsync(c => c.TicketId != null && c.ExternalId == commentExtId && c.Source == CommentSource.GitHub, ct);

                    if (!exists)
                    {
                        var ticket = existingTicket ?? await db.Tickets.FirstOrDefaultAsync(t => t.ProjectId == project.Id && t.ExternalId == externalId, ct);
                        if (ticket != null)
                        {
                            db.Comments.Add(new DomainComment
                            {
                                Id = Guid.NewGuid(),
                                TicketId = ticket.Id,
                                AuthorId = fallbackAuthorId,
                                Content = ghComment.Body,
                                Source = CommentSource.GitHub,
                                ExternalId = commentExtId,
                                ExternalUser = ghComment.User?.Login,
                                CreatedAt = DateTime.UtcNow
                            });
                            synced++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to sync GitHub issue #{IssueNumber}", issue.Number);
                failed++;
            }
        }

        await db.SaveChangesAsync(ct);
        return (synced, failed);
    }
}
