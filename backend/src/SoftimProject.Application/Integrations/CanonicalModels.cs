namespace SoftimProject.Application.Integrations;

// Provider-agnostic ("canonical") representation of data pulled from an external
// project system (EasyProject, Jira, Redmine, ...). Each ISourceConnector maps its
// own API models onto these records so the shared sync logic never depends on a
// specific system. External identifiers are strings (Jira uses non-numeric ids;
// numeric sources stringify their id). Rich text is carried as-is from the source
// (e.g. HTML for EasyProject) — normalization to Markdown is the engine's job.

public enum CanonicalProjectStatus
{
    Active,
    Completed,
    Archived
}

public enum CanonicalFieldFormat
{
    Text,
    Number,
    Date,
    Select
}

/// <summary>Lightweight reference to a person in the source system (assignee, author, …).</summary>
public sealed record CanonicalUserRef(string ExternalId, string? DisplayName);

public sealed record CanonicalUser(
    string ExternalId,
    string? Login,
    string? FirstName,
    string? LastName,
    string? Email,
    string? AvatarUrl);

/// <summary>A single status / priority / type option as defined in the source system.</summary>
public sealed record CanonicalLookup(string ExternalId, string Name, bool IsClosed);

/// <summary>The source system's lookup catalogues, used to build mappings to ProjectMan lookups.</summary>
public sealed record CanonicalLookups(
    IReadOnlyList<CanonicalLookup> Types,
    IReadOnlyList<CanonicalLookup> Statuses,
    IReadOnlyList<CanonicalLookup> Priorities);

public sealed record CanonicalCustomFieldValue(
    string ExternalFieldId,
    string Name,
    string? Value,
    CanonicalFieldFormat Format,
    IReadOnlyList<string>? Options);

public sealed record CanonicalProject(
    string ExternalId,
    string Name,
    string? DescriptionHtml,
    CanonicalProjectStatus Status,
    string? ParentExternalId,
    string? StartDate,
    string? DueDate,
    IReadOnlyList<CanonicalCustomFieldValue> CustomFields,
    // Source-system project code/slug/key (EP identifier, Jira key, ...). Preferred base for the
    // ProjectMan project code on import; null → the code is derived from the name.
    string? SourceCode = null);

public sealed record CanonicalComment(
    string ExternalId,
    CanonicalUserRef? Author,
    string? BodyHtml,
    bool IsInternal,
    DateTime? CreatedAt);

public sealed record CanonicalAttachment(
    string ExternalId,
    string FileName,
    long FileSizeBytes,
    string? ContentType,
    string ContentUrl,
    DateTime? CreatedAt);

public sealed record CanonicalChecklistItem(
    string ExternalId,
    string Text,
    int Position,
    bool IsCompleted);

public sealed record CanonicalIssue(
    string ExternalId,
    string Title,
    string? DescriptionHtml,
    string? TypeExternalId,
    string? StatusExternalId,
    string? StatusName,
    string? PriorityExternalId,
    CanonicalUserRef? Assignee,
    CanonicalUserRef? Reporter,
    decimal? EstimatedHours,
    string? DueDate,
    string? ParentExternalId,
    string? ProjectExternalId,
    string? ProjectName,
    IReadOnlyList<CanonicalCustomFieldValue> CustomFields,
    IReadOnlyList<CanonicalComment> Comments,
    IReadOnlyList<CanonicalAttachment> Attachments,
    IReadOnlyList<CanonicalChecklistItem> ChecklistItems,
    // Direct link to the issue in the source system's web UI. Built by the connector
    // (provider-specific URL shape), so the engine never hard-codes a path.
    string? WebUrl = null,
    // When the record was last modified in the source system. Drives the SourceOwnedWins
    // conflict policy (overwrite only when the source actually changed). Null = unknown.
    DateTime? SourceUpdatedAt = null);

public sealed record CanonicalWorklog(
    string ExternalId,
    string? IssueExternalId,
    CanonicalUserRef? User,
    string? SpentOn,
    decimal Hours,
    string? CommentHtml,
    bool IsBillable);
