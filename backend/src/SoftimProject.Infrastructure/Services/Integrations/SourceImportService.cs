using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Features.Integration;
using SoftimProject.Application.Interfaces;
using SoftimProject.Application.Common;

namespace SoftimProject.Infrastructure.Services.Integrations;

/// <summary>
/// Provider-agnostic one-time import. Resolves the connector by SourceSystem and runs the
/// shared <see cref="SyncEngine"/> with a <see cref="MigrationJobSink"/> (so progress shows in
/// the migration UI). The canonical request is built straight from the string-keyed command.
/// </summary>
public sealed class SourceImportService(
    IApplicationDbContext dbContext,
    SyncEngine syncEngine,
    IEnumerable<ISourceConnector> connectors,
    IMigrationNotifier notifier) : ISourceImportService
{
    public async Task ExecuteAsync(Guid jobId, StartSourceImportCommand cmd, Guid integrationConnectionId)
    {
        var connector = connectors.FirstOrDefault(c => c.SourceSystem == cmd.SourceSystem)
            ?? throw new InvalidOperationException($"No connector registered for {cmd.SourceSystem}.");

        var adminUserId = await dbContext.MigrationJobs
            .Where(j => j.Id == jobId)
            .Select(j => j.InitiatedByUserId)
            .FirstOrDefaultAsync();
        if (adminUserId == Guid.Empty)
            throw new NotFoundException(nameof(Domain.Entities.MigrationJob), jobId);

        var sink = new MigrationJobSink(dbContext, notifier, jobId);
        var context = new SourceConnectionContext(cmd.BaseUrl, cmd.ApiKey);
        var request = new SyncEngineRequest(
            cmd.TargetProjectTemplateId,
            cmd.ProjectExternalIds,
            cmd.TrackerMapping,
            cmd.StatusMapping,
            cmd.PriorityMapping,
            cmd.UserMapping,
            cmd.SkipClosedIssues,
            cmd.SkipAttachments,
            cmd.ImportComments,
            cmd.ImportWorklogs,
            cmd.ImportChecklists,
            cmd.CreateMissingUsers,
            cmd.AutoCreateTrackers,
            cmd.AutoCreateStatuses,
            cmd.AutoCreateStatusIsClosed,
            cmd.AutoCreatePriorities,
            ChangedSince: null,
            IntegrationConnectionId: integrationConnectionId,
            TargetCompanyId: cmd.TargetCompanyId);

        await syncEngine.ExecuteAsync(jobId, adminUserId, request, connector, context, sink);
    }
}
