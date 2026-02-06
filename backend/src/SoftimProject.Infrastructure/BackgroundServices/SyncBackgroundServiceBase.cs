using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.BackgroundServices;

public abstract class SyncBackgroundServiceBase(
    IServiceScopeFactory scopeFactory,
    ILogger logger,
    TimeSpan interval,
    SyncType syncType) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                await ExecuteSyncAsync(scope.ServiceProvider, dbContext, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{ServiceName} failed", GetType().Name);
            }
        }
    }

    protected abstract Task ExecuteSyncAsync(IServiceProvider services, IApplicationDbContext dbContext, CancellationToken cancellationToken);

    protected async Task LogSyncAsync(IApplicationDbContext dbContext, Guid projectId, SyncStatus status, int synced, int failed, string? error, CancellationToken cancellationToken)
    {
        dbContext.SyncLogs.Add(new SyncLog
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SyncType = syncType,
            Status = status,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            ItemsSynced = synced,
            ItemsFailed = failed,
            ErrorMessage = error
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
