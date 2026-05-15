using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Services.Email;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class EmailPollingService(
    IServiceScopeFactory scopeFactory,
    IJobRegistry jobRegistry,
    ILogger<EmailPollingService> logger)
    : TrackedBackgroundService(scopeFactory, jobRegistry, logger, TimeSpan.FromMinutes(2))
{
    protected override async Task ExecuteIterationAsync(
        IServiceProvider services,
        IJobRunScope run,
        CancellationToken cancellationToken)
    {
        var options = services.GetRequiredService<IOptions<EmailSyncOptions>>().Value;
        if (!options.Enabled)
        {
            run.MarkSuccess(itemsProcessed: 0);
            return;
        }

        var db = services.GetRequiredService<IApplicationDbContext>();
        var mailbox = services.GetRequiredService<IEmailMailboxClient>();

        var result = await EmailSyncHelper.SyncAsync(
            db, mailbox, options.AliasPrefix, options.BatchSize, logger, cancellationToken);

        foreach (var (projectId, counters) in result.PerProject)
        {
            if (counters.Synced == 0 && counters.Failed == 0) continue;
            var status = counters.Failed == 0
                ? SyncStatus.Success
                : counters.Synced == 0 ? SyncStatus.Failed : SyncStatus.PartialSuccess;
            await SyncLogHelper.WriteAsync(db, projectId, SyncType.Email, status,
                counters.Synced, counters.Failed, null, cancellationToken);
        }

        run.MarkSuccess(result.TotalSynced, result.TotalFailed);
    }
}
