using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Projects.GitHub;

// Convention-based mapping of GitHub issue metadata ↔ ticket fields (#110).
//   - assignee  ↔ ticket.AssigneeId  (GitHub login matched to User.GitHubLogin)
//   - label     ↔ ticket.TicketPriorityId  (label name matched to a priority name; template-scoped)
//   - label     ↔ ticket.TaskTypeId  (label name matched to a task-type name; task types are global)
// Until a per-template mapping table + UI lands, matching is by name (case-insensitive,
// against Name / NameCs / NameEn), mirroring PullRequestStatusMapper. Labels may carry a
// "prefix: value" convention (e.g. "priority: high", "type/bug") — the value part is matched.
// Unmapped values are silently ignored, so a free-form GitHub repo never corrupts ticket data.
//
// Lives in Application (not Infrastructure) so the background sync helper, the manual
// TriggerGitHubSyncCommand, and the webhook controller can all share one implementation.
public static class GitHubMetadataMapper
{
    // Strips a leading "prefix:" / "prefix/" segment and trims, so "priority: High" → "High".
    public static string Normalize(string raw)
    {
        var s = raw.Trim();
        var idx = s.LastIndexOfAny([':', '/']);
        if (idx >= 0 && idx < s.Length - 1)
            s = s[(idx + 1)..].Trim();
        return s;
    }

    private static bool Matches(string normalizedLabel, params string?[] names)
        => names.Any(n => !string.IsNullOrWhiteSpace(n)
            && string.Equals(n!.Trim(), normalizedLabel, StringComparison.OrdinalIgnoreCase));

    // GitHub assignee logins → ticket assignee user id (first matching user wins).
    public static async Task<Guid?> ResolveAssigneeIdAsync(
        IApplicationDbContext db,
        IReadOnlyCollection<string> logins,
        CancellationToken ct)
    {
        if (logins.Count == 0) return null;
        var lowered = logins.Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.ToLowerInvariant()).Distinct().ToList();
        if (lowered.Count == 0) return null;

        return await db.Users
            .Where(u => u.GitHubLogin != null && lowered.Contains(u.GitHubLogin.ToLower()))
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);
    }

    // Issue labels → ticket priority id (priorities are template-scoped).
    public static async Task<Guid?> ResolvePriorityIdAsync(
        IApplicationDbContext db,
        Guid? projectTemplateId,
        IReadOnlyCollection<string> labels,
        CancellationToken ct)
    {
        if (labels.Count == 0) return null;
        var q = db.TicketPriorities.Where(p => p.IsActive);
        if (projectTemplateId.HasValue)
            q = q.Where(p => p.ProjectTemplateId == projectTemplateId.Value);

        var priorities = await q
            .OrderBy(p => p.SortOrder)
            .Select(p => new { p.Id, p.Name, p.NameCs, p.NameEn })
            .ToListAsync(ct);

        var normalized = labels.Select(Normalize).ToList();
        var match = priorities.FirstOrDefault(p =>
            normalized.Any(n => Matches(n, p.Name, p.NameCs, p.NameEn)));
        return match?.Id;
    }

    // Issue labels → ticket task type id (task types are global, not template-scoped).
    public static async Task<Guid?> ResolveTaskTypeIdAsync(
        IApplicationDbContext db,
        IReadOnlyCollection<string> labels,
        CancellationToken ct)
    {
        if (labels.Count == 0) return null;

        var types = await db.TaskTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .Select(t => new { t.Id, t.Name, t.NameCs, t.NameEn })
            .ToListAsync(ct);

        var normalized = labels.Select(Normalize).ToList();
        var match = types.FirstOrDefault(t =>
            normalized.Any(n => Matches(n, t.Name, t.NameCs, t.NameEn)));
        return match?.Id;
    }

    // Ticket assignee id → GitHub login for the outbound push (null when none / not linked).
    public static async Task<string?> ResolveAssigneeLoginAsync(
        IApplicationDbContext db,
        Guid? assigneeId,
        CancellationToken ct)
    {
        if (assigneeId is null) return null;
        return await db.Users
            .Where(u => u.Id == assigneeId.Value && u.GitHubLogin != null)
            .Select(u => u.GitHubLogin)
            .FirstOrDefaultAsync(ct);
    }
}
