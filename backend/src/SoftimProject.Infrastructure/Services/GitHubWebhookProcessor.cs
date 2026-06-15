using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Application.Features.Projects.GitHub;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;
using DomainProject = SoftimProject.Domain.Entities.Project;
using DomainTicket = SoftimProject.Domain.Entities.Ticket;
using DomainComment = SoftimProject.Domain.Entities.Comment;
using DomainLinkedPr = SoftimProject.Domain.Entities.LinkedPullRequest;
using DomainLinkedCommit = SoftimProject.Domain.Entities.LinkedCommit;

namespace SoftimProject.Infrastructure.Services;

// Applies a parsed GitHub webhook payload to the domain. Self-contained (opens its own
// DbContext scope) so both the live endpoint and the DLQ replay handler can drive it.
public sealed class GitHubWebhookProcessor(IServiceScopeFactory scopeFactory) : IGitHubWebhookProcessor
{
    public async Task<WebhookProcessResult> ProcessAsync(string eventType, string body, CancellationToken cancellationToken = default)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (!root.TryGetProperty("repository", out var repoEl) || !repoEl.TryGetProperty("full_name", out var fullNameEl))
            return new WebhookProcessResult(false, "Missing repository info");

        var repoFullName = fullNameEl.GetString();

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.ExternalSystem == "GitHub" && p.ExternalProjectId == repoFullName, cancellationToken);
        if (project == null)
            return new WebhookProcessResult(false, "No project configured for this repository");

        var defaultStateId = await db.TaskStates
            .Where(ts => ts.IsActive && ts.IsDefault).Select(ts => ts.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (defaultStateId == Guid.Empty)
            defaultStateId = await db.TaskStates.Where(ts => ts.IsActive).OrderBy(ts => ts.SortOrder).Select(ts => ts.Id).FirstAsync(cancellationToken);

        var closedStateId = await db.TaskStates
            .Where(ts => ts.IsActive && ts.IsClosedState).Select(ts => ts.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (closedStateId == Guid.Empty)
            closedStateId = defaultStateId;

        var defaultPriorityId = await db.TicketPriorities
            .Where(tp => tp.IsActive && tp.IsDefault).Select(tp => tp.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (defaultPriorityId == Guid.Empty)
            defaultPriorityId = await db.TicketPriorities.Where(tp => tp.IsActive).OrderBy(tp => tp.SortOrder).Select(tp => tp.Id).FirstAsync(cancellationToken);

        var fallbackAuthorId = await db.ProjectMembers
            .Where(pm => pm.ProjectId == project.Id).OrderBy(pm => pm.Id).Select(pm => pm.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (fallbackAuthorId == Guid.Empty)
            fallbackAuthorId = await db.Users.Select(u => u.Id).FirstOrDefaultAsync(cancellationToken);

        switch (eventType)
        {
            case "issues":
                await HandleIssueEvent(db, root, project, fallbackAuthorId, defaultStateId, closedStateId, defaultPriorityId, cancellationToken);
                break;
            case "issue_comment":
                await HandleIssueCommentEvent(db, root, project, fallbackAuthorId, cancellationToken);
                break;
            case "pull_request":
                await HandlePullRequestEvent(db, root, project, fallbackAuthorId, cancellationToken);
                break;
            case "push":
                await HandlePushEvent(db, root, project, closedStateId, cancellationToken);
                break;
            case "check_suite":
            case "check_run":
                await HandleCheckSuiteEvent(db, root, project, cancellationToken);
                break;
            case "status":
                await HandleStatusEvent(db, root, project, cancellationToken);
                break;
            default:
                return new WebhookProcessResult(true, $"Event '{eventType}' ignored");
        }

        await db.SaveChangesAsync(cancellationToken);
        return new WebhookProcessResult(true, "Processed");
    }

    // "fixes PROJ-42", "closes #5", "resolved …" — used to auto-close on default-branch push.
    private static readonly Regex FixKeyword = new(
        @"\b(close[sd]?|fix(e[sd])?|resolve[sd]?)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static async Task HandlePushEvent(IApplicationDbContext db, JsonElement root, DomainProject project, Guid closedStateId, CancellationToken ct)
    {
        if (!root.TryGetProperty("commits", out var commits) || commits.ValueKind != JsonValueKind.Array)
            return;

        var refName = root.TryGetProperty("ref", out var refEl) ? refEl.GetString() : null;
        var defaultBranch = root.TryGetProperty("repository", out var repoEl)
            && repoEl.TryGetProperty("default_branch", out var dbEl) ? dbEl.GetString() : null;
        var onDefaultBranch = refName != null && defaultBranch != null
            && string.Equals(refName, $"refs/heads/{defaultBranch}", StringComparison.Ordinal);

        foreach (var commit in commits.EnumerateArray())
        {
            var sha = commit.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
            var message = commit.TryGetProperty("message", out var msgEl) ? msgEl.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(sha) || string.IsNullOrWhiteSpace(message))
                continue;

            var key = GitHubTicketResolver.TryResolve(message);
            if (key is null || !string.Equals(key.ProjectCode, project.Code, StringComparison.OrdinalIgnoreCase))
                continue;

            var ticket = await db.Tickets
                .Include(t => t.TaskState)
                .FirstOrDefaultAsync(t => t.ProjectId == project.Id && t.Number == key.TicketNumber, ct);
            if (ticket is null)
                continue;

            var url = commit.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? "" : "";
            var author = commit.TryGetProperty("author", out var authEl) && authEl.TryGetProperty("username", out var unEl)
                ? unEl.GetString() : null;
            var committedAt = commit.TryGetProperty("timestamp", out var tsEl) && tsEl.TryGetDateTime(out var ts)
                ? ts : DateTime.UtcNow;
            var trimmedMessage = message.Length > 1000 ? message[..1000] : message;

            var existing = await db.LinkedCommits.FirstOrDefaultAsync(
                c => c.Provider == "GitHub" && c.Sha == sha && c.TicketId == ticket.Id, ct);
            if (existing is null)
            {
                db.LinkedCommits.Add(new DomainLinkedCommit
                {
                    Id = Guid.NewGuid(),
                    TicketId = ticket.Id,
                    Provider = "GitHub",
                    Sha = sha,
                    Message = trimmedMessage,
                    Url = url,
                    AuthorLogin = author,
                    CommittedAt = committedAt,
                });
            }
            else
            {
                existing.Message = trimmedMessage;
                existing.Url = url;
                existing.AuthorLogin = author;
            }

            // "fixes/closes" on the default branch closes the ticket.
            if (onDefaultBranch && closedStateId != Guid.Empty
                && !ticket.TaskState.IsClosedState && FixKeyword.IsMatch(message))
            {
                ticket.TaskStateId = closedStateId;
                ticket.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    private static async Task HandleIssueEvent(IApplicationDbContext db, JsonElement root, DomainProject project, Guid fallbackAuthorId,
        Guid defaultStateId, Guid closedStateId, Guid defaultPriorityId, CancellationToken ct)
    {
        var action = root.GetProperty("action").GetString();
        var issue = root.GetProperty("issue");
        var issueNumber = issue.GetProperty("number").GetInt32().ToString();
        var title = issue.GetProperty("title").GetString() ?? "";
        var bodyText = issue.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() : null;
        var htmlUrl = issue.GetProperty("html_url").GetString();
        var userLogin = issue.TryGetProperty("user", out var userEl) && userEl.TryGetProperty("login", out var loginEl) ? loginEl.GetString() : null;

        // --- Metadata mapping (#110): label → priority/type, assignee → řešitel ---
        var labelNames = new List<string>();
        if (issue.TryGetProperty("labels", out var labelsEl) && labelsEl.ValueKind == JsonValueKind.Array)
            foreach (var l in labelsEl.EnumerateArray())
                if (l.TryGetProperty("name", out var nameEl) && nameEl.GetString() is { } ln)
                    labelNames.Add(ln);

        var assigneeLogins = new List<string>();
        if (issue.TryGetProperty("assignees", out var assigneesEl) && assigneesEl.ValueKind == JsonValueKind.Array)
            foreach (var a in assigneesEl.EnumerateArray())
                if (a.TryGetProperty("login", out var alEl) && alEl.GetString() is { } al)
                    assigneeLogins.Add(al);

        var mappedAssignee = await GitHubMetadataMapper.ResolveAssigneeIdAsync(db, assigneeLogins, ct);
        var mappedPriority = await GitHubMetadataMapper.ResolvePriorityIdAsync(db, project.ProjectTemplateId, labelNames, ct);
        var mappedType = await GitHubMetadataMapper.ResolveTaskTypeIdAsync(db, labelNames, ct);

        var existingTicket = await db.Tickets
            .Include(t => t.TaskState)
            .FirstOrDefaultAsync(t => t.ProjectId == project.Id && t.ExternalId == issueNumber, ct);

        switch (action)
        {
            case "opened" when existingTicket == null:
                db.Tickets.Add(new DomainTicket
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    Title = title,
                    Description = bodyText,
                    TicketPriorityId = mappedPriority ?? defaultPriorityId,
                    TaskTypeId = mappedType,
                    AssigneeId = mappedAssignee,
                    TaskStateId = defaultStateId,
                    ReporterId = fallbackAuthorId,
                    ExternalId = issueNumber,
                    ExternalUrl = htmlUrl,
                    ExternalUser = userLogin,
                    Position = 0,
                    CreatedAt = DateTime.UtcNow
                });
                break;

            case "edited" when existingTicket != null:
            case "labeled" when existingTicket != null:
            case "unlabeled" when existingTicket != null:
            case "assigned" when existingTicket != null:
            case "unassigned" when existingTicket != null:
                // Equality guard: only touch the ticket when something actually changed.
                // Prevents a ping-pong loop with our own outbound ticket→issue sync.
                var changed = false;
                if (existingTicket.Title != title) { existingTicket.Title = title; changed = true; }
                if (existingTicket.Description != bodyText) { existingTicket.Description = bodyText; changed = true; }
                if (mappedAssignee != null && existingTicket.AssigneeId != mappedAssignee)
                { existingTicket.AssigneeId = mappedAssignee; changed = true; }
                if (mappedPriority != null && existingTicket.TicketPriorityId != mappedPriority.Value)
                { existingTicket.TicketPriorityId = mappedPriority.Value; changed = true; }
                if (mappedType != null && existingTicket.TaskTypeId != mappedType)
                { existingTicket.TaskTypeId = mappedType; changed = true; }
                if (changed)
                    existingTicket.UpdatedAt = DateTime.UtcNow;
                break;

            case "closed" when existingTicket != null:
                if (!existingTicket.TaskState.IsClosedState)
                {
                    existingTicket.TaskStateId = closedStateId;
                    existingTicket.UpdatedAt = DateTime.UtcNow;
                }
                break;

            case "reopened" when existingTicket != null:
                if (existingTicket.TaskState.IsClosedState)
                {
                    existingTicket.TaskStateId = defaultStateId;
                    existingTicket.UpdatedAt = DateTime.UtcNow;
                }
                break;
        }
    }

    private static async Task HandleIssueCommentEvent(IApplicationDbContext db, JsonElement root, DomainProject project, Guid fallbackAuthorId, CancellationToken ct)
    {
        var action = root.GetProperty("action").GetString();
        if (action != "created") return;

        var issue = root.GetProperty("issue");
        var issueNumber = issue.GetProperty("number").GetInt32().ToString();
        var comment = root.GetProperty("comment");
        var commentId = comment.GetProperty("id").GetInt64().ToString();
        var commentBody = comment.GetProperty("body").GetString() ?? "";
        var userLogin = comment.TryGetProperty("user", out var userEl) && userEl.TryGetProperty("login", out var loginEl) ? loginEl.GetString() : null;

        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.ProjectId == project.Id && t.ExternalId == issueNumber, ct);
        if (ticket == null) return;

        var exists = await db.Comments
            .AnyAsync(c => c.ExternalId == commentId && c.Source == CommentSource.GitHub, ct);
        if (!exists)
        {
            db.Comments.Add(new DomainComment
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                AuthorId = fallbackAuthorId,
                Content = commentBody,
                Source = CommentSource.GitHub,
                ExternalId = commentId,
                ExternalUser = userLogin,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private static async Task HandlePullRequestEvent(IApplicationDbContext db, JsonElement root, DomainProject project, Guid fallbackAuthorId, CancellationToken ct)
    {
        var action = root.GetProperty("action").GetString();
        var pr = root.GetProperty("pull_request");
        var prNumber = pr.GetProperty("number").GetInt32().ToString();
        var prTitle = pr.GetProperty("title").GetString() ?? "";
        var htmlUrl = pr.GetProperty("html_url").GetString() ?? "";
        var branch = pr.GetProperty("head").GetProperty("ref").GetString() ?? "";
        var body = pr.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() : null;
        var merged = pr.TryGetProperty("merged", out var mergedEl) && mergedEl.GetBoolean();
        var commitsCount = pr.TryGetProperty("commits", out var commitsEl) && commitsEl.TryGetInt32(out var cc) ? cc : 0;
        var userLogin = pr.TryGetProperty("user", out var userEl) && userEl.TryGetProperty("login", out var loginEl)
            ? loginEl.GetString() : null;
        var createdAt = pr.TryGetProperty("created_at", out var createdEl) && createdEl.TryGetDateTime(out var createdDt)
            ? createdDt : DateTime.UtcNow;
        var closedAt = pr.TryGetProperty("closed_at", out var closedEl) && closedEl.ValueKind != JsonValueKind.Null
            && closedEl.TryGetDateTime(out var closedDt) ? (DateTime?)closedDt : null;
        var mergedAt = pr.TryGetProperty("merged_at", out var mergedAtEl) && mergedAtEl.ValueKind != JsonValueKind.Null
            && mergedAtEl.TryGetDateTime(out var mergedDt) ? (DateTime?)mergedDt : null;

        // Resolve target ticket from branch name or PR body / title — first match wins.
        var key = GitHubTicketResolver.TryResolve(branch, prTitle, body);
        if (key is null || !string.Equals(key.ProjectCode, project.Code, StringComparison.OrdinalIgnoreCase))
            return;

        var ticket = await db.Tickets
            .Include(t => t.TaskState)
            .FirstOrDefaultAsync(t => t.ProjectId == project.Id && t.Number == key.TicketNumber, ct);
        if (ticket is null) return;

        var state = merged ? PullRequestState.Merged
            : action == "closed" ? PullRequestState.Closed
            : PullRequestState.Open;

        var trimmedBody = body != null && body.Length > 4000 ? body[..4000] : body;

        // Upsert by (Provider, ExternalId, TicketId) — webhook replays update the row.
        var existing = await db.LinkedPullRequests.FirstOrDefaultAsync(
            lp => lp.Provider == "GitHub" && lp.ExternalId == prNumber && lp.TicketId == ticket.Id, ct);
        if (existing is null)
        {
            db.LinkedPullRequests.Add(new DomainLinkedPr
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                Provider = "GitHub",
                ExternalId = prNumber,
                Url = htmlUrl,
                Title = prTitle,
                Branch = branch,
                Description = trimmedBody,
                CommitsCount = commitsCount,
                AuthorLogin = userLogin,
                State = state,
                OpenedAt = createdAt,
                ClosedAt = closedAt,
                MergedAt = mergedAt,
            });
        }
        else
        {
            existing.Url = htmlUrl;
            existing.Title = prTitle;
            existing.Branch = branch;
            existing.Description = trimmedBody;
            if (commitsCount > 0) existing.CommitsCount = commitsCount;
            existing.AuthorLogin = userLogin;
            existing.State = state;
            existing.ClosedAt = closedAt;
            existing.MergedAt = mergedAt;
        }

        // Status transition (convention-based; see PullRequestStatusMapper).
        if (action == "opened")
        {
            var reviewStateId = await PullRequestStatusMapper
                .FindReviewStateIdAsync(db, ticket.Project?.ProjectTemplateId, ct);
            if (reviewStateId.HasValue && ticket.TaskStateId != reviewStateId.Value)
            {
                ticket.TaskStateId = reviewStateId.Value;
                ticket.UpdatedAt = DateTime.UtcNow;
            }
            AddSystemComment(db, ticket, $"PR #{prNumber} opened: {prTitle} — {htmlUrl}", fallbackAuthorId);
        }
        else if (action == "closed" && merged)
        {
            var closedStateId = await PullRequestStatusMapper
                .FindClosedStateIdAsync(db, ticket.Project?.ProjectTemplateId, ct);
            if (closedStateId.HasValue && ticket.TaskStateId != closedStateId.Value)
            {
                ticket.TaskStateId = closedStateId.Value;
                ticket.UpdatedAt = DateTime.UtcNow;
            }
            AddSystemComment(db, ticket, $"PR #{prNumber} merged: {prTitle} — {htmlUrl}", fallbackAuthorId);
        }
    }

    // check_suite / check_run events carry the aggregate CI conclusion and the PRs they
    // belong to. We stamp ChecksStatus on every linked PR row for those PR numbers.
    private static async Task HandleCheckSuiteEvent(IApplicationDbContext db, JsonElement root, DomainProject project, CancellationToken ct)
    {
        var suite = root.TryGetProperty("check_suite", out var csEl) ? csEl
            : root.TryGetProperty("check_run", out var crEl) && crEl.TryGetProperty("check_suite", out var nestedEl) ? nestedEl
            : default;
        if (suite.ValueKind != JsonValueKind.Object) return;

        var conclusion = suite.TryGetProperty("conclusion", out var concEl) && concEl.ValueKind == JsonValueKind.String
            ? concEl.GetString() : null;
        var status = suite.TryGetProperty("status", out var statEl) && statEl.ValueKind == JsonValueKind.String
            ? statEl.GetString() : null;
        var checksStatus = (conclusion ?? status)?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(checksStatus)) return;

        if (!suite.TryGetProperty("pull_requests", out var prs) || prs.ValueKind != JsonValueKind.Array)
            return;

        foreach (var prEl in prs.EnumerateArray())
        {
            if (!prEl.TryGetProperty("number", out var numEl) || !numEl.TryGetInt32(out var num)) continue;
            var prNumber = num.ToString();
            var rows = await db.LinkedPullRequests
                .Where(lp => lp.Provider == "GitHub" && lp.ExternalId == prNumber && lp.Ticket!.ProjectId == project.Id)
                .ToListAsync(ct);
            foreach (var row in rows)
                row.ChecksStatus = checksStatus;
        }
    }

    // status events are keyed by commit + branch, not PR number. Match linked PRs by branch.
    private static async Task HandleStatusEvent(IApplicationDbContext db, JsonElement root, DomainProject project, CancellationToken ct)
    {
        var state = root.TryGetProperty("state", out var stateEl) ? stateEl.GetString()?.ToLowerInvariant() : null;
        if (string.IsNullOrWhiteSpace(state)) return;

        if (!root.TryGetProperty("branches", out var branches) || branches.ValueKind != JsonValueKind.Array)
            return;

        foreach (var br in branches.EnumerateArray())
        {
            var name = br.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(name)) continue;

            var rows = await db.LinkedPullRequests
                .Where(lp => lp.Provider == "GitHub" && lp.Branch == name && lp.Ticket!.ProjectId == project.Id)
                .ToListAsync(ct);
            foreach (var row in rows)
                row.ChecksStatus = state;
        }
    }

    private static void AddSystemComment(IApplicationDbContext db, DomainTicket ticket, string content, Guid authorId)
    {
        if (authorId == Guid.Empty) return;
        db.Comments.Add(new DomainComment
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ProjectId = ticket.ProjectId,
            AuthorId = authorId,
            Content = content,
            Source = CommentSource.GitHub,
            CreatedAt = DateTime.UtcNow,
        });
    }
}
