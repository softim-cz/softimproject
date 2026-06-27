using SoftimProject.Application.Integrations;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Interfaces;

/// <summary>
/// Read-only contract for a source project system (EasyProject, Jira, Redmine, …).
/// Each provider implements this once and maps its own API/models onto the canonical
/// model, so the shared sync engine never depends on a specific system. Adding a new
/// system = one new connector, not a new pipeline.
/// </summary>
/// <remarks>
/// Pass <c>changedSince = null</c> for a full pull (one-time import); a non-null value
/// requests only records changed at/after that instant (incremental sync).
/// </remarks>
public interface ISourceConnector
{
    /// <summary>Which source system this connector talks to.</summary>
    SyncType SourceSystem { get; }

    Task<(bool Success, string? Error)> TestConnectionAsync(SourceConnectionContext context, CancellationToken ct);

    Task<IReadOnlyList<CanonicalProject>> GetProjectsAsync(SourceConnectionContext context, CancellationToken ct);

    Task<IReadOnlyList<CanonicalUser>> GetUsersAsync(SourceConnectionContext context, CancellationToken ct);

    Task<CanonicalLookups> GetLookupsAsync(SourceConnectionContext context, CancellationToken ct);

    Task<IReadOnlyList<CanonicalIssue>> GetIssuesAsync(SourceConnectionContext context, string projectExternalId, DateTime? changedSince, CancellationToken ct);

    Task<IReadOnlyList<CanonicalWorklog>> GetWorklogsAsync(SourceConnectionContext context, string projectExternalId, DateTime? changedSince, CancellationToken ct);

    Task<Stream> DownloadAttachmentAsync(SourceConnectionContext context, string contentUrl, CancellationToken ct);
}

/// <summary>Connection details for a source system (later sourced from IntegrationConnection).</summary>
public sealed record SourceConnectionContext(string BaseUrl, string ApiToken);
