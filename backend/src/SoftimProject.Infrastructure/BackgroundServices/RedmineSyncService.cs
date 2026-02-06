using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class RedmineSyncService(IServiceScopeFactory scopeFactory, ILogger<RedmineSyncService> logger)
    : SyncBackgroundServiceBase(scopeFactory, logger, TimeSpan.FromMinutes(5), SyncType.Redmine)
{
    protected override async Task ExecuteSyncAsync(IServiceProvider services, IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var projects = await dbContext.Projects
            .Where(p => p.ExternalSystem == "Redmine" && p.ExternalProjectId != null && p.Status == ProjectStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var project in projects)
        {
            try
            {
                // TODO: Implement actual Redmine REST API calls
                logger.LogInformation("Redmine sync completed for project {ProjectCode}", project.Code);
                await LogSyncAsync(dbContext, project.Id, SyncStatus.Success, 0, 0, null, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Redmine sync failed for project {ProjectCode}", project.Code);
                await LogSyncAsync(dbContext, project.Id, SyncStatus.Failed, 0, 0, ex.Message, cancellationToken);
            }
        }
    }
}
