using SoftimProject.Application.Features.Integration;
using SoftimProject.Application.Features.Migration.EasyProject;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Interfaces;

/// <summary>
/// Persists (upserts) the <c>IntegrationConnection</c> for a migration so repeated and
/// incremental syncs can reuse its credentials and mappings. The API token is encrypted
/// via <see cref="ISecretProtector"/>; scheduling fields (Mode/Interval/IsEnabled) are set
/// only on creation and preserved on update (owned by the user afterwards).
/// </summary>
public interface IIntegrationConnectionWriter
{
    /// <summary>Upserts the EasyProject connection for the given migration command. Returns its id.</summary>
    Task<Guid> UpsertForEasyProjectAsync(StartMigrationCommand command, Guid createdByUserId, CancellationToken ct);

    /// <summary>Upserts a connection for any source system from the provider-agnostic import command.</summary>
    Task<Guid> UpsertForImportAsync(StartSourceImportCommand command, Guid createdByUserId, CancellationToken ct);

    /// <summary>
    /// Remembers a bare connection (system + URL + token) right after a successful connection test,
    /// before any import is configured. On an existing connection only the token is refreshed.
    /// </summary>
    Task<Guid> RememberConnectionAsync(SyncType system, string baseUrl, string apiKey, Guid createdByUserId, CancellationToken ct);
}
