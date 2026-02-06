using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class JiraSyncService(IServiceScopeFactory scopeFactory, ILogger<JiraSyncService> logger)
    : SyncBackgroundServiceBase(scopeFactory, logger, TimeSpan.FromMinutes(5), SyncType.Jira)
{
    protected override async Task ExecuteSyncAsync(IServiceProvider services, IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var projects = await dbContext.Projects
            .Where(p => p.ExternalSystem == "Jira" && p.ExternalProjectId != null && p.Status == ProjectStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var project in projects)
        {
            try
            {
                // TODO: Implement actual Jira REST API calls
                // 1. Fetch issues updated since last sync
                // 2. Map Jira issues to tickets
                // 3. Create or update tickets
                // 4. Sync comments
                logger.LogInformation("Jira sync completed for project {ProjectCode}", project.Code);
                await LogSyncAsync(dbContext, project.Id, SyncStatus.Success, 0, 0, null, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Jira sync failed for project {ProjectCode}", project.Code);
                await LogSyncAsync(dbContext, project.Id, SyncStatus.Failed, 0, 0, ex.Message, cancellationToken);
            }
        }
    }
}
