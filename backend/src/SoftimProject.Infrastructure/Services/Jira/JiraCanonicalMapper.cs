using SoftimProject.Application.Integrations;
using SoftimProject.Application.Integrations.Jira;

namespace SoftimProject.Infrastructure.Services.Jira;

/// <summary>
/// Pure mapping of Jira Cloud REST v3 models onto the canonical model. Side-effect free and
/// unit-tested. Rich content (comments/worklogs/attachments, which require ADF handling and
/// per-issue calls) is out of scope for the initial connector and mapped as empty.
/// </summary>
public static class JiraCanonicalMapper
{
    public static CanonicalProject MapProject(JiraProject p) => new(
        p.Id,
        p.Name ?? p.Key ?? p.Id,
        p.Description,
        CanonicalProjectStatus.Active,
        ParentExternalId: null,
        StartDate: null,
        DueDate: null,
        CustomFields: []);

    public static CanonicalUserRef? MapUserRef(JiraUser? user) =>
        user?.AccountId is { } id ? new CanonicalUserRef(id, user.DisplayName) : null;

    public static CanonicalUser? MapUser(JiraUser? user) =>
        user?.AccountId is { } id ? new CanonicalUser(id, null, null, null, user.EmailAddress, null) : null;

    public static CanonicalLookups MapLookups(
        IEnumerable<JiraNamedEntity> issueTypes,
        IEnumerable<JiraStatus> statuses,
        IEnumerable<JiraNamedEntity> priorities) => new(
            issueTypes.Select(t => new CanonicalLookup(t.Id, t.Name ?? t.Id, false)).ToList(),
            statuses.Select(s => new CanonicalLookup(s.Id, s.Name ?? s.Id, IsClosedStatus(s))).ToList(),
            priorities.Select(p => new CanonicalLookup(p.Id, p.Name ?? p.Id, false)).ToList());

    public static bool IsClosedStatus(JiraStatus status) =>
        string.Equals(status.StatusCategory?.Key, "done", StringComparison.OrdinalIgnoreCase);

    public static CanonicalIssue MapIssue(JiraIssue issue, string baseUrl)
    {
        var f = issue.Fields;
        return new CanonicalIssue(
            issue.Id,
            f?.Summary ?? string.Empty,
            // renderedFields.description is HTML (when expand=renderedFields) → engine converts to Markdown.
            issue.RenderedFields?.Description,
            f?.IssueType?.Id,
            f?.Status?.Id,
            f?.Status?.Name,
            f?.Priority?.Id,
            MapUserRef(f?.Assignee),
            MapUserRef(f?.Reporter),
            SecondsToHours(f?.TimeOriginalEstimate),
            f?.DueDate,
            f?.Parent?.Id,
            f?.Project?.Id,
            f?.Project?.Name,
            CustomFields: [],
            Comments: [],
            Attachments: [],
            ChecklistItems: [],
            WebUrl: $"{baseUrl.TrimEnd('/')}/browse/{issue.Key}",
            SourceUpdatedAt: ParseDateTime(f?.Updated));
    }

    public static decimal? SecondsToHours(long? seconds) =>
        seconds is { } s ? Math.Round(s / 3600m, 2) : null;

    private static DateTime? ParseDateTime(string? dt) =>
        string.IsNullOrWhiteSpace(dt) ? null : DateTime.TryParse(dt, out var d) ? d.ToUniversalTime() : null;
}
