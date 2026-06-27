using SoftimProject.Application.Features.Migration.EasyProject;

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
    Task<Guid> UpsertForEasyProjectAsync(StartMigrationCommand command, CancellationToken ct);
}
