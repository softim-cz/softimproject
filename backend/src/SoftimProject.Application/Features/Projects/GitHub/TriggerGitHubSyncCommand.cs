using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Octokit;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.GitHub;

public sealed record TriggerGitHubSyncCommand(Guid ProjectId) : IRequest<TriggerGitHubSyncResult>, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.ProjectManager;
}

public sealed record TriggerGitHubSyncResult(int Synced, int Failed, string? Error);

public sealed class TriggerGitHubSyncCommandHandler(
    IApplicationDbContext dbContext,
    IGitHubAppTokenService appTokenService,
    ILogger<TriggerGitHubSyncCommandHandler> logger) : IRequestHandler<TriggerGitHubSyncCommand, TriggerGitHubSyncResult>
{
    public async Task<TriggerGitHubSyncResult> Handle(TriggerGitHubSyncCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new Common.NotFoundException(nameof(Domain.Entities.Project), request.ProjectId);

        if (project.ExternalSystem != "GitHub" || string.IsNullOrWhiteSpace(project.ExternalProjectId))
            return new TriggerGitHubSyncResult(0, 0, "GitHub integration not configured for this project");

        // Resolve token: prefer a GitHub App installation token (server-to-server),
        // then the connected user's OAuth token, then a legacy PAT.
        string? token = null;
        if (appTokenService.IsConfigured && project.GitHubInstallationId.HasValue)
            token = await appTokenService.GetInstallationTokenAsync(project.GitHubInstallationId.Value, cancellationToken);
        token ??= project.ExternalApiToken;
        if (string.IsNullOrWhiteSpace(token) && project.GitHubConnectedByUserId.HasValue)
        {
            token = await dbContext.Users
                .Where(u => u.Id == project.GitHubConnectedByUserId.Value)
                .Select(u => u.GitHubAccessToken)
                .FirstOrDefaultAsync(cancellationToken);
        }
        if (string.IsNullOrWhiteSpace(token))
            return new TriggerGitHubSyncResult(0, 0, "No GitHub access token available");

        var parts = project.ExternalProjectId.Split('/');
        if (parts.Length != 2)
            return new TriggerGitHubSyncResult(0, 0, "Invalid repository format");

        // Resolve default TaskState and TicketPriority IDs
        var defaultStateId = await dbContext.TaskStates
            .Where(ts => ts.IsActive && ts.IsDefault)
            .Select(ts => ts.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (defaultStateId == Guid.Empty)
            defaultStateId = await dbContext.TaskStates.Where(ts => ts.IsActive).OrderBy(ts => ts.SortOrder).Select(ts => ts.Id).FirstAsync(cancellationToken);

        var closedStateId = await dbContext.TaskStates
            .Where(ts => ts.IsActive && ts.IsClosedState)
            .Select(ts => ts.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (closedStateId == Guid.Empty)
            closedStateId = defaultStateId;

        var defaultPriorityId = await dbContext.TicketPriorities
            .Where(tp => tp.IsActive && tp.IsDefault)
            .Select(tp => tp.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (defaultPriorityId == Guid.Empty)
            defaultPriorityId = await dbContext.TicketPriorities.Where(tp => tp.IsActive).OrderBy(tp => tp.SortOrder).Select(tp => tp.Id).FirstAsync(cancellationToken);

        try
        {
            var client = new GitHubClient(new ProductHeaderValue("SoftimProject"))
            {
                Credentials = new Credentials(token)
            };

            var lastSync = await dbContext.SyncLogs
                .Where(s => s.ProjectId == project.Id && s.SyncType == SyncType.GitHub && s.Status == SyncStatus.Success)
                .OrderByDescending(s => s.CompletedAt)
                .Select(s => s.CompletedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var synced = 0;
            var failed = 0;
            var owner = parts[0];
            var repo = parts[1];

            var fallbackAuthorId = await dbContext.ProjectMembers
                .Where(pm => pm.ProjectId == project.Id)
                .OrderBy(pm => pm.Id)
                .Select(pm => pm.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (fallbackAuthorId == Guid.Empty)
                fallbackAuthorId = await dbContext.Users.Select(u => u.Id).FirstOrDefaultAsync(cancellationToken);

            // --- Outbound: Push new tickets to GitHub ---
            var outboundTickets = await dbContext.Tickets
                .Include(t => t.TaskState)
                .Where(t => t.ProjectId == project.Id && t.ExternalId == null)
                .ToListAsync(cancellationToken);

            foreach (var ticket in outboundTickets)
            {
                try
                {
                    var newIssue = new NewIssue(ticket.Title) { Body = ticket.Description ?? "" };
                    var created = await client.Issue.Create(owner, repo, newIssue);
                    ticket.ExternalId = created.Number.ToString();
                    ticket.ExternalUrl = created.HtmlUrl;
                    synced++;

                    if (ticket.TaskState.IsClosedState)
                        await client.Issue.Update(owner, repo, created.Number, new IssueUpdate { State = ItemState.Closed });
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to push ticket {TicketId} to GitHub", ticket.Id);
                    failed++;
                }
            }

            // --- Outbound: Push comments ---
            var ticketsWithExternalId = await dbContext.Tickets
                .Where(t => t.ProjectId == project.Id && t.ExternalId != null)
                .Select(t => new { t.Id, t.ExternalId })
                .ToListAsync(cancellationToken);

            var ticketIds = ticketsWithExternalId.Select(t => t.Id).ToList();

            var outboundComments = await dbContext.Comments
                .Where(c => c.TicketId != null && ticketIds.Contains(c.TicketId!.Value) && c.Source != CommentSource.GitHub && c.ExternalId == null)
                .ToListAsync(cancellationToken);

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
            var changedLinkedTickets = await dbContext.Tickets
                .Include(t => t.TaskState)
                .Where(t => t.ProjectId == project.Id
                    && t.ExternalId != null
                    && (lastSync == null || t.UpdatedAt > lastSync))
                .ToListAsync(cancellationToken);

            foreach (var ticket in changedLinkedTickets)
            {
                if (!int.TryParse(ticket.ExternalId, out var issueNumber)) continue;
                try
                {
                    await client.Issue.Update(owner, repo, issueNumber, new IssueUpdate
                    {
                        Title = ticket.Title,
                        Body = ticket.Description ?? "",
                        State = ticket.TaskState.IsClosedState ? ItemState.Closed : ItemState.Open,
                    });
                    synced++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to push ticket {TicketId} lifecycle to GitHub issue #{IssueNumber}", ticket.Id, issueNumber);
                    failed++;
                }
            }

            // --- Inbound: Pull issues from GitHub ---
            var issueRequest = new RepositoryIssueRequest
            {
                State = ItemStateFilter.All,
                SortProperty = IssueSort.Updated,
                SortDirection = SortDirection.Descending
            };
            if (lastSync.HasValue)
                issueRequest.Since = new DateTimeOffset(lastSync.Value, TimeSpan.Zero);

            var issues = await client.Issue.GetAllForRepository(owner, repo, issueRequest);

            foreach (var issue in issues)
            {
                if (issue.PullRequest != null) continue;

                try
                {
                    var externalId = issue.Number.ToString();
                    var existingTicket = await dbContext.Tickets
                        .Include(t => t.TaskState)
                        .FirstOrDefaultAsync(t => t.ProjectId == project.Id && t.ExternalId == externalId, cancellationToken);

                    if (existingTicket != null)
                    {
                        // Equality guard so the outbound lifecycle push doesn't loop.
                        var changed = false;
                        if (existingTicket.Title != issue.Title) { existingTicket.Title = issue.Title; changed = true; }
                        if (existingTicket.Description != issue.Body) { existingTicket.Description = issue.Body; changed = true; }
                        if (existingTicket.ExternalUrl != issue.HtmlUrl) { existingTicket.ExternalUrl = issue.HtmlUrl; changed = true; }

                        if (issue.State.Value == ItemState.Closed && !existingTicket.TaskState.IsClosedState)
                        { existingTicket.TaskStateId = closedStateId; changed = true; }
                        else if (issue.State.Value == ItemState.Open && existingTicket.TaskState.IsClosedState)
                        { existingTicket.TaskStateId = defaultStateId; changed = true; }

                        if (changed)
                            existingTicket.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        dbContext.Tickets.Add(new Ticket
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = project.Id,
                            Title = issue.Title,
                            Description = issue.Body,
                            TicketPriorityId = defaultPriorityId,
                            TaskStateId = issue.State.Value == ItemState.Closed ? closedStateId : defaultStateId,
                            ReporterId = fallbackAuthorId,
                            ExternalId = externalId,
                            ExternalUrl = issue.HtmlUrl,
                            ExternalUser = issue.User?.Login,
                            Position = 0,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    synced++;

                    // Inbound comments
                    var issueComments = await client.Issue.Comment.GetAllForIssue(owner, repo, issue.Number);
                    foreach (var ghComment in issueComments)
                    {
                        var commentExtId = ghComment.Id.ToString();
                        var exists = await dbContext.Comments
                            .AnyAsync(c => c.ExternalId == commentExtId && c.Source == CommentSource.GitHub, cancellationToken);

                        if (!exists)
                        {
                            var ticket = existingTicket ?? await dbContext.Tickets.FirstOrDefaultAsync(t => t.ProjectId == project.Id && t.ExternalId == externalId, cancellationToken);
                            if (ticket != null)
                            {
                                dbContext.Comments.Add(new Comment
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

            await dbContext.SaveChangesAsync(cancellationToken);

            // Log the sync
            dbContext.SyncLogs.Add(new SyncLog
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                SyncType = SyncType.GitHub,
                Status = SyncStatus.Success,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                ItemsSynced = synced,
                ItemsFailed = failed
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            return new TriggerGitHubSyncResult(synced, failed, null);
        }
        catch (Exception ex)
        {
            return new TriggerGitHubSyncResult(0, 0, ex.Message);
        }
    }
}
