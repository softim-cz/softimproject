using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Octokit;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Services;
using DomainProject = SoftimProject.Domain.Entities.Project;
using DomainTicket = SoftimProject.Domain.Entities.Ticket;
using DomainComment = SoftimProject.Domain.Entities.Comment;
using DomainLinkedPr = SoftimProject.Domain.Entities.LinkedPullRequest;

namespace SoftimProject.WebApi.Controllers;

[Route("api/webhooks")]
[ApiController]
[AllowAnonymous]
public class WebhooksController(
    IApplicationDbContext dbContext) : ControllerBase
{
    [HttpPost("github")]
    public async Task<IActionResult> GitHubWebhook(CancellationToken ct)
    {
        // Read raw body
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        var eventType = Request.Headers["X-GitHub-Event"].FirstOrDefault();
        if (string.IsNullOrEmpty(eventType))
            return BadRequest("Missing X-GitHub-Event header");

        // Parse the payload to get repository info
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (!root.TryGetProperty("repository", out var repoEl) || !repoEl.TryGetProperty("full_name", out var fullNameEl))
            return BadRequest("Missing repository info");

        var repoFullName = fullNameEl.GetString();

        // Find matching project
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.ExternalSystem == "GitHub" && p.ExternalProjectId == repoFullName, ct);

        if (project == null)
            return NotFound("No project configured for this repository");

        // Verify webhook signature
        if (!string.IsNullOrEmpty(project.WebhookSecret))
        {
            var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            if (!VerifySignature(body, signature, project.WebhookSecret))
                return Unauthorized("Invalid webhook signature");
        }

        // Resolve default TaskState and TicketPriority IDs
        var defaultStateId = await dbContext.TaskStates
            .Where(ts => ts.IsActive && ts.IsDefault)
            .Select(ts => ts.Id)
            .FirstOrDefaultAsync(ct);
        if (defaultStateId == Guid.Empty)
            defaultStateId = await dbContext.TaskStates.Where(ts => ts.IsActive).OrderBy(ts => ts.SortOrder).Select(ts => ts.Id).FirstAsync(ct);

        var closedStateId = await dbContext.TaskStates
            .Where(ts => ts.IsActive && ts.IsClosedState)
            .Select(ts => ts.Id)
            .FirstOrDefaultAsync(ct);
        if (closedStateId == Guid.Empty)
            closedStateId = defaultStateId;

        var defaultPriorityId = await dbContext.TicketPriorities
            .Where(tp => tp.IsActive && tp.IsDefault)
            .Select(tp => tp.Id)
            .FirstOrDefaultAsync(ct);
        if (defaultPriorityId == Guid.Empty)
            defaultPriorityId = await dbContext.TicketPriorities.Where(tp => tp.IsActive).OrderBy(tp => tp.SortOrder).Select(tp => tp.Id).FirstAsync(ct);

        // Process events
        var fallbackAuthorId = await dbContext.ProjectMembers
            .Where(pm => pm.ProjectId == project.Id)
            .OrderBy(pm => pm.Id)
            .Select(pm => pm.UserId)
            .FirstOrDefaultAsync(ct);

        if (fallbackAuthorId == Guid.Empty)
            fallbackAuthorId = await dbContext.Users.Select(u => u.Id).FirstOrDefaultAsync(ct);

        switch (eventType)
        {
            case "issues":
                await HandleIssueEvent(root, project, fallbackAuthorId, defaultStateId, closedStateId, defaultPriorityId, ct);
                break;
            case "issue_comment":
                await HandleIssueCommentEvent(root, project, fallbackAuthorId, ct);
                break;
            case "pull_request":
                await HandlePullRequestEvent(root, project, fallbackAuthorId, ct);
                break;
            default:
                return Ok(new { message = $"Event '{eventType}' ignored" });
        }

        await dbContext.SaveChangesAsync(ct);
        return Ok(new { message = "Processed" });
    }

    private async Task HandleIssueEvent(JsonElement root, DomainProject project, Guid fallbackAuthorId,
        Guid defaultStateId, Guid closedStateId, Guid defaultPriorityId, CancellationToken ct)
    {
        var action = root.GetProperty("action").GetString();
        var issue = root.GetProperty("issue");
        var issueNumber = issue.GetProperty("number").GetInt32().ToString();
        var title = issue.GetProperty("title").GetString() ?? "";
        var bodyText = issue.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() : null;
        var htmlUrl = issue.GetProperty("html_url").GetString();
        var state = issue.GetProperty("state").GetString();
        var userLogin = issue.TryGetProperty("user", out var userEl) && userEl.TryGetProperty("login", out var loginEl) ? loginEl.GetString() : null;

        var existingTicket = await dbContext.Tickets
            .Include(t => t.TaskState)
            .FirstOrDefaultAsync(t => t.ProjectId == project.Id && t.ExternalId == issueNumber, ct);

        switch (action)
        {
            case "opened" when existingTicket == null:
                dbContext.Tickets.Add(new DomainTicket
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    Title = title,
                    Description = bodyText,
                    TicketPriorityId = defaultPriorityId,
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
                // Equality guard: only touch the ticket when something actually changed.
                // Prevents a ping-pong loop with our own outbound ticket→issue sync.
                if (existingTicket.Title != title || existingTicket.Description != bodyText)
                {
                    existingTicket.Title = title;
                    existingTicket.Description = bodyText;
                    existingTicket.UpdatedAt = DateTime.UtcNow;
                }
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

    private async Task HandleIssueCommentEvent(JsonElement root, DomainProject project, Guid fallbackAuthorId, CancellationToken ct)
    {
        var action = root.GetProperty("action").GetString();
        if (action != "created") return;

        var issue = root.GetProperty("issue");
        var issueNumber = issue.GetProperty("number").GetInt32().ToString();
        var comment = root.GetProperty("comment");
        var commentId = comment.GetProperty("id").GetInt64().ToString();
        var commentBody = comment.GetProperty("body").GetString() ?? "";
        var userLogin = comment.TryGetProperty("user", out var userEl) && userEl.TryGetProperty("login", out var loginEl) ? loginEl.GetString() : null;

        var ticket = await dbContext.Tickets
            .FirstOrDefaultAsync(t => t.ProjectId == project.Id && t.ExternalId == issueNumber, ct);

        if (ticket == null) return;

        var exists = await dbContext.Comments
            .AnyAsync(c => c.ExternalId == commentId && c.Source == CommentSource.GitHub, ct);

        if (!exists)
        {
            dbContext.Comments.Add(new DomainComment
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

    private async Task HandlePullRequestEvent(
        JsonElement root,
        DomainProject project,
        Guid fallbackAuthorId,
        CancellationToken ct)
    {
        var action = root.GetProperty("action").GetString();
        var pr = root.GetProperty("pull_request");
        var prNumber = pr.GetProperty("number").GetInt32().ToString();
        var prTitle = pr.GetProperty("title").GetString() ?? "";
        var htmlUrl = pr.GetProperty("html_url").GetString() ?? "";
        var branch = pr.GetProperty("head").GetProperty("ref").GetString() ?? "";
        var body = pr.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() : null;
        var merged = pr.TryGetProperty("merged", out var mergedEl) && mergedEl.GetBoolean();
        var userLogin = pr.TryGetProperty("user", out var userEl) && userEl.TryGetProperty("login", out var loginEl)
            ? loginEl.GetString()
            : null;
        var createdAt = pr.TryGetProperty("created_at", out var createdEl) && createdEl.TryGetDateTime(out var createdDt)
            ? createdDt
            : DateTime.UtcNow;
        var closedAt = pr.TryGetProperty("closed_at", out var closedEl) && closedEl.ValueKind != JsonValueKind.Null
            && closedEl.TryGetDateTime(out var closedDt) ? (DateTime?)closedDt : null;
        var mergedAt = pr.TryGetProperty("merged_at", out var mergedAtEl) && mergedAtEl.ValueKind != JsonValueKind.Null
            && mergedAtEl.TryGetDateTime(out var mergedDt) ? (DateTime?)mergedDt : null;

        // Resolve target ticket from branch name or PR body / title — first match wins.
        var key = GitHubTicketResolver.TryResolve(branch, prTitle, body);
        if (key is null || !string.Equals(key.ProjectCode, project.Code, StringComparison.OrdinalIgnoreCase))
            return;

        var ticket = await dbContext.Tickets
            .Include(t => t.TaskState)
            .FirstOrDefaultAsync(t => t.ProjectId == project.Id && t.Number == key.TicketNumber, ct);
        if (ticket is null) return;

        var state = merged ? PullRequestState.Merged
            : action == "closed" ? PullRequestState.Closed
            : PullRequestState.Open;

        // Upsert by (Provider, ExternalId, TicketId) — webhook replays update the row.
        var existing = await dbContext.LinkedPullRequests.FirstOrDefaultAsync(
            lp => lp.Provider == "GitHub" && lp.ExternalId == prNumber && lp.TicketId == ticket.Id, ct);
        if (existing is null)
        {
            dbContext.LinkedPullRequests.Add(new DomainLinkedPr
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                Provider = "GitHub",
                ExternalId = prNumber,
                Url = htmlUrl,
                Title = prTitle,
                Branch = branch,
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
            existing.AuthorLogin = userLogin;
            existing.State = state;
            existing.ClosedAt = closedAt;
            existing.MergedAt = mergedAt;
        }

        // Status transition (convention-based; see PullRequestStatusMapper).
        //   - PR opened → "In Review" if such state exists for the project template
        //   - PR merged → closed state (IsClosedState=true)
        // Re-opens and plain closes leave the ticket state untouched.
        if (action == "opened")
        {
            var reviewStateId = await PullRequestStatusMapper
                .FindReviewStateIdAsync(dbContext, ticket.Project?.ProjectTemplateId, ct);
            if (reviewStateId.HasValue && ticket.TaskStateId != reviewStateId.Value)
            {
                ticket.TaskStateId = reviewStateId.Value;
                ticket.UpdatedAt = DateTime.UtcNow;
            }

            AddSystemCommentAsync(ticket, $"PR #{prNumber} opened: {prTitle} — {htmlUrl}", fallbackAuthorId);
        }
        else if (action == "closed" && merged)
        {
            var closedStateId = await PullRequestStatusMapper
                .FindClosedStateIdAsync(dbContext, ticket.Project?.ProjectTemplateId, ct);
            if (closedStateId.HasValue && ticket.TaskStateId != closedStateId.Value)
            {
                ticket.TaskStateId = closedStateId.Value;
                ticket.UpdatedAt = DateTime.UtcNow;
            }

            AddSystemCommentAsync(ticket, $"PR #{prNumber} merged: {prTitle} — {htmlUrl}", fallbackAuthorId);
        }
    }

    private void AddSystemCommentAsync(DomainTicket ticket, string content, Guid authorId)
    {
        if (authorId == Guid.Empty) return;
        dbContext.Comments.Add(new DomainComment
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

    private static bool VerifySignature(string payload, string? signature, string secret)
    {
        if (string.IsNullOrEmpty(signature)) return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expected = "sha256=" + Convert.ToHexStringLower(hash);
        return string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase);
    }
}
