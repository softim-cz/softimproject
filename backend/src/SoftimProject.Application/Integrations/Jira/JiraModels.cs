namespace SoftimProject.Application.Integrations.Jira;

// Minimal Jira Cloud REST v3 response shapes consumed by the connector. Deserialized with
// camelCase + case-insensitive options. Only the fields the canonical mapper needs.

public sealed record JiraSearchResponse(int StartAt, int MaxResults, int Total, List<JiraIssue>? Issues);

public sealed record JiraIssue(string Id, string Key, JiraFields? Fields, JiraRenderedFields? RenderedFields);

public sealed record JiraFields(
    string? Summary,
    JiraRef? IssueType,
    JiraStatus? Status,
    JiraRef? Priority,
    JiraUser? Assignee,
    JiraUser? Reporter,
    long? TimeOriginalEstimate,
    string? DueDate,
    JiraParent? Parent,
    JiraProjectRef? Project,
    string? Updated);

public sealed record JiraRenderedFields(string? Description);

public sealed record JiraRef(string Id, string? Name);

public sealed record JiraStatus(string Id, string? Name, JiraStatusCategory? StatusCategory);

public sealed record JiraStatusCategory(string? Key); // "done" => closed state

public sealed record JiraUser(string? AccountId, string? DisplayName, string? EmailAddress);

public sealed record JiraParent(string Id, string? Key);

public sealed record JiraProjectRef(string Id, string? Key, string? Name);

public sealed record JiraProjectSearchResponse(int StartAt, int MaxResults, int Total, List<JiraProject>? Values);

public sealed record JiraProject(string Id, string? Key, string? Name, string? Description);

public sealed record JiraNamedEntity(string Id, string? Name); // issuetype / priority lists
