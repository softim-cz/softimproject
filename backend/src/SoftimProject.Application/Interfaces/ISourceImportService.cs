using SoftimProject.Application.Features.Integration;

namespace SoftimProject.Application.Interfaces;

/// <summary>
/// Runs a provider-agnostic one-time import for the given job: resolves the connector by the
/// command's SourceSystem and drives the shared SyncEngine, reporting onto the MigrationJob.
/// </summary>
public interface ISourceImportService
{
    Task ExecuteAsync(Guid jobId, StartSourceImportCommand command, Guid integrationConnectionId);
}
