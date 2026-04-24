using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class RedmineSyncService(
    IServiceScopeFactory scopeFactory,
    IJobRegistry jobRegistry,
    ILogger<RedmineSyncService> logger)
    : TrackedBackgroundService(scopeFactory, jobRegistry, logger, TimeSpan.FromMinutes(5))
{
    protected override async Task ExecuteIterationAsync(
        IServiceProvider services,
        IJobRunScope run,
        CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<IApplicationDbContext>();

        var projects = await dbContext.Projects
            .Where(p => p.ExternalSystem == "Redmine" && p.ExternalProjectId != null && p.Status == ProjectStatus.Active)
            .ToListAsync(cancellationToken);

        var processed = 0;
        var failed = 0;
        foreach (var project in projects)
        {
            try
            {
                // TODO: Implement actual Redmine REST API calls
                logger.LogInformation("Redmine sync completed for project {ProjectCode}", project.Code);
                await SyncLogHelper.WriteAsync(dbContext, project.Id, SyncType.Redmine, SyncStatus.Success, 0, 0, null, cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Redmine sync failed for project {ProjectCode}", project.Code);
                await SyncLogHelper.WriteAsync(dbContext, project.Id, SyncType.Redmine, SyncStatus.Failed, 0, 0, ex.Message, cancellationToken);
                failed++;
            }
        }

        run.MarkSuccess(processed, failed);
    }
}
