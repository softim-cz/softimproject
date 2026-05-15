using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;
using DomainComment = SoftimProject.Domain.Entities.Comment;
using DomainProject = SoftimProject.Domain.Entities.Project;
using DomainTicket = SoftimProject.Domain.Entities.Ticket;

namespace SoftimProject.Infrastructure.Services.Email;

public static partial class EmailSyncHelper
{
    [GeneratedRegex(@"\[#(?<code>[A-Z][A-Z0-9]{1,5})-(?<num>\d+)\]", RegexOptions.CultureInvariant)]
    private static partial Regex ReplyTokenRegex();

    public static async Task<EmailSyncResult> SyncAsync(
        IApplicationDbContext db,
        IEmailMailboxClient mailbox,
        string aliasPrefix,
        int batchSize,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Load enabled email-mapped projects up front; if none, skip the Graph call entirely.
        var projects = await db.Projects
            .Where(p => p.ExternalSystem == "Email"
                && p.ExternalProjectId != null
                && p.Status == ProjectStatus.Active)
            .ToListAsync(cancellationToken);

        if (projects.Count == 0)
        {
            return EmailSyncResult.Empty;
        }

        var aliasMap = projects.ToDictionary(
            p => p.ExternalProjectId!.ToLowerInvariant(),
            p => p);

        var messages = await mailbox.FetchUnreadAsync(batchSize, cancellationToken);
        if (messages.Count == 0)
        {
            return EmailSyncResult.Empty;
        }

        // Resolve a default TaskState + TicketPriority per template (cached).
        // Key Guid.Empty represents "no template" (project.ProjectTemplateId is null).
        var stateCache = new Dictionary<Guid, Guid>();
        var priorityCache = new Dictionary<Guid, Guid>();
        var memberCache = new Dictionary<Guid, Guid>();
        var perProject = projects.ToDictionary(p => p.Id, _ => new ProjectCounters());

        foreach (var msg in messages)
        {
            try
            {
                var project = ResolveProject(msg, aliasPrefix, aliasMap);
                if (project is null)
                {
                    logger.LogDebug("Skipping email {MessageId}: no recipient matched alias map", msg.Id);
                    await mailbox.MarkAsReadAsync(msg.Id, cancellationToken);
                    continue;
                }

                var counters = perProject[project.Id];

                // Idempotency: never re-import the same Graph message id.
                var alreadyImported = await db.Comments
                    .AnyAsync(c => c.Source == CommentSource.Email && c.ExternalId == msg.Id, cancellationToken)
                    || await db.Tickets
                        .AnyAsync(t => t.ExternalId == msg.Id && t.ProjectId == project.Id, cancellationToken);

                if (alreadyImported)
                {
                    await mailbox.MarkAsReadAsync(msg.Id, cancellationToken);
                    continue;
                }

                var fallbackAuthorId = await ResolveFallbackAuthorAsync(db, project.Id, memberCache, cancellationToken);
                if (fallbackAuthorId == Guid.Empty)
                {
                    logger.LogWarning("Skipping email {MessageId}: project {ProjectCode} has no members and no users in system", msg.Id, project.Code);
                    counters.Failed++;
                    continue;
                }

                var replyTicket = await TryFindReplyTargetAsync(db, project, msg.Subject, cancellationToken);
                if (replyTicket is not null)
                {
                    db.Comments.Add(new DomainComment
                    {
                        Id = Guid.NewGuid(),
                        TicketId = replyTicket.Id,
                        AuthorId = fallbackAuthorId,
                        Content = PlainText(msg.Body),
                        Source = CommentSource.Email,
                        ExternalId = msg.Id,
                        ExternalUser = msg.FromAddress,
                        CreatedAt = DateTime.UtcNow,
                    });
                }
                else
                {
                    var stateId = await ResolveDefaultStateAsync(db, project.ProjectTemplateId, stateCache, cancellationToken);
                    var priorityId = await ResolveDefaultPriorityAsync(db, project.ProjectTemplateId, priorityCache, cancellationToken);
                    if (stateId == Guid.Empty || priorityId == Guid.Empty)
                    {
                        logger.LogWarning("Skipping email {MessageId}: project {ProjectCode} has no default TaskState or TicketPriority", msg.Id, project.Code);
                        counters.Failed++;
                        continue;
                    }

                    db.Tickets.Add(new DomainTicket
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = project.Id,
                        Number = project.NextTicketNumber++,
                        Title = Truncate(msg.Subject, 500),
                        Description = PlainText(msg.Body),
                        TicketPriorityId = priorityId,
                        TaskStateId = stateId,
                        ReporterId = fallbackAuthorId,
                        ExternalId = msg.Id,
                        ExternalUser = msg.FromAddress,
                        Position = 0,
                        CreatedAt = DateTime.UtcNow,
                    });
                }

                await db.SaveChangesAsync(cancellationToken);
                await mailbox.MarkAsReadAsync(msg.Id, cancellationToken);
                counters.Synced++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to process email {MessageId}", msg.Id);
                // Don't mark as read on failure — next iteration retries.
                // Attribute to all matched projects? No; attribute to none if we couldn't even resolve a project.
                // If project was resolved, the inner block already incremented Failed; this catches earlier failures.
            }
        }

        return new EmailSyncResult(perProject);
    }

    private static DomainProject? ResolveProject(
        EmailMessage msg,
        string aliasPrefix,
        Dictionary<string, DomainProject> aliasMap)
    {
        foreach (var addr in msg.ToRecipients.Concat(msg.CcRecipients))
        {
            var at = addr.IndexOf('@');
            if (at <= 0) continue;
            var local = addr[..at];
            if (!local.StartsWith(aliasPrefix, StringComparison.OrdinalIgnoreCase)) continue;
            var key = local[aliasPrefix.Length..].ToLowerInvariant();
            if (aliasMap.TryGetValue(key, out var project)) return project;
        }
        return null;
    }

    private static async Task<DomainTicket?> TryFindReplyTargetAsync(
        IApplicationDbContext db,
        DomainProject project,
        string subject,
        CancellationToken ct)
    {
        var match = ReplyTokenRegex().Match(subject);
        if (!match.Success) return null;
        if (!string.Equals(match.Groups["code"].Value, project.Code, StringComparison.OrdinalIgnoreCase)) return null;
        if (!int.TryParse(match.Groups["num"].Value, out var number)) return null;

        return await db.Tickets
            .FirstOrDefaultAsync(t => t.ProjectId == project.Id && t.Number == number, ct);
    }

    private static async Task<Guid> ResolveDefaultStateAsync(
        IApplicationDbContext db,
        Guid? templateId,
        Dictionary<Guid, Guid> cache,
        CancellationToken ct)
    {
        var key = templateId ?? Guid.Empty;
        if (cache.TryGetValue(key, out var cached)) return cached;

        var query = db.TaskStates.Where(ts => ts.IsActive);
        if (templateId.HasValue)
            query = query.Where(ts => ts.ProjectTemplateId == templateId.Value);

        var id = await query.Where(ts => ts.IsDefault).Select(ts => ts.Id).FirstOrDefaultAsync(ct);
        if (id == Guid.Empty)
            id = await query.OrderBy(ts => ts.SortOrder).Select(ts => ts.Id).FirstOrDefaultAsync(ct);

        cache[key] = id;
        return id;
    }

    private static async Task<Guid> ResolveDefaultPriorityAsync(
        IApplicationDbContext db,
        Guid? templateId,
        Dictionary<Guid, Guid> cache,
        CancellationToken ct)
    {
        var key = templateId ?? Guid.Empty;
        if (cache.TryGetValue(key, out var cached)) return cached;

        var query = db.TicketPriorities.Where(p => p.IsActive);
        if (templateId.HasValue)
            query = query.Where(p => p.ProjectTemplateId == templateId.Value);

        var id = await query.Where(p => p.IsDefault).Select(p => p.Id).FirstOrDefaultAsync(ct);
        if (id == Guid.Empty)
            id = await query.OrderBy(p => p.SortOrder).Select(p => p.Id).FirstOrDefaultAsync(ct);

        cache[key] = id;
        return id;
    }

    private static async Task<Guid> ResolveFallbackAuthorAsync(
        IApplicationDbContext db,
        Guid projectId,
        Dictionary<Guid, Guid> cache,
        CancellationToken ct)
    {
        if (cache.TryGetValue(projectId, out var cached)) return cached;

        var memberId = await db.ProjectMembers
            .Where(pm => pm.ProjectId == projectId)
            .OrderBy(pm => pm.Id)
            .Select(pm => pm.UserId)
            .FirstOrDefaultAsync(ct);

        if (memberId == Guid.Empty)
            memberId = await db.Users.OrderBy(u => u.Id).Select(u => u.Id).FirstOrDefaultAsync(ct);

        cache[projectId] = memberId;
        return memberId;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static string PlainText(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;
        // Graph returns HTML when contentType=html. Strip tags conservatively;
        // not perfect but enough for ticket descriptions / comments.
        var stripped = HtmlTagRegex().Replace(body, " ");
        return Truncate(System.Net.WebUtility.HtmlDecode(stripped).Trim(), 10000);
    }

    [GeneratedRegex(@"<[^>]+>", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();
}

public sealed class ProjectCounters
{
    public int Synced { get; set; }
    public int Failed { get; set; }
}

public sealed record EmailSyncResult(IReadOnlyDictionary<Guid, ProjectCounters> PerProject)
{
    public static EmailSyncResult Empty { get; } = new(new Dictionary<Guid, ProjectCounters>());

    public int TotalSynced => PerProject.Values.Sum(c => c.Synced);
    public int TotalFailed => PerProject.Values.Sum(c => c.Failed);
}
