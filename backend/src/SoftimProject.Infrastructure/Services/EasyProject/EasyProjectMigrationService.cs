using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Migration.EasyProject;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Services.Integrations;

namespace SoftimProject.Infrastructure.Services.EasyProject;

/// <summary>
/// Thin adapter that keeps the existing <see cref="IEasyProjectMigrationService"/> entry
/// point (called by Start/Resume command handlers) while delegating the actual work to
/// the provider-agnostic <see cref="SyncEngine"/> driven by the EasyProject
/// <see cref="ISourceConnector"/>. It only translates the int-keyed
/// <see cref="StartMigrationCommand"/> into the string-keyed <see cref="SyncEngineRequest"/>;
/// behavior is unchanged.
/// </summary>
public sealed class EasyProjectMigrationService : IEasyProjectMigrationService
{
    private readonly SyncEngine _syncEngine;
    private readonly ISourceConnector _connector;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMigrationNotifier _notifier;

    public EasyProjectMigrationService(
        SyncEngine syncEngine,
        IEnumerable<ISourceConnector> connectors,
        IApplicationDbContext dbContext,
        IMigrationNotifier notifier)
    {
        _syncEngine = syncEngine;
        _connector = connectors.First(c => c.SourceSystem == SyncType.EasyProject);
        _dbContext = dbContext;
        _notifier = notifier;
    }

    public async Task ExecuteAsync(Guid jobId, StartMigrationCommand cmd, Guid? integrationConnectionId)
    {
        // The wizard run is recorded on its MigrationJob (status/phase/progress over SignalR).
        var adminUserId = await _dbContext.MigrationJobs
            .Where(j => j.Id == jobId)
            .Select(j => j.InitiatedByUserId)
            .FirstOrDefaultAsync();
        if (adminUserId == Guid.Empty)
            throw new NotFoundException(nameof(Domain.Entities.MigrationJob), jobId);

        var sink = new MigrationJobSink(_dbContext, _notifier, jobId);
        var context = new SourceConnectionContext(cmd.BaseUrl, cmd.ApiKey);
        var request = new SyncEngineRequest(
            cmd.TargetProjectTemplateId,
            cmd.ProjectIds.Select(id => id.ToString()).ToList(),
            cmd.TrackerMapping.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            cmd.StatusMapping.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            cmd.PriorityMapping.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            cmd.UserMapping.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            cmd.SkipClosedIssues,
            cmd.SkipAttachments,
            cmd.ImportComments,
            cmd.ImportWorklogs,
            cmd.ImportChecklists,
            cmd.CreateMissingUsers,
            cmd.AutoCreateTrackers?.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            cmd.AutoCreateStatuses?.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            cmd.AutoCreateStatusIsClosed?.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            cmd.AutoCreatePriorities?.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            ChangedSince: null,
            IntegrationConnectionId: integrationConnectionId);

        await _syncEngine.ExecuteAsync(jobId, adminUserId, request, _connector, context, sink);
    }
}
