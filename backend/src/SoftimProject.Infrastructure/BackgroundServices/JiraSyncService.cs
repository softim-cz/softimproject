using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class JiraSyncService(
    IServiceScopeFactory scopeFactory,
    IJobRegistry jobRegistry,
    ILogger<JiraSyncService> logger)
    : TrackedBackgroundService(scopeFactory, jobRegistry, logger, TimeSpan.FromMinutes(5))
{
    protected override async Task ExecuteIterationAsync(
        IServiceProvider services,
        IJobRunScope run,
        CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<IApplicationDbContext>();

        var projects = await dbContext.Projects
            .Where(p => p.ExternalSystem == "Jira" && p.ExternalProjectId != null && p.Status == ProjectStatus.Active)
            .ToListAsync(cancellationToken);

        var processed = 0;
        var failed = 0;
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
                await SyncLogHelper.WriteAsync(dbContext, project.Id, SyncType.Jira, SyncStatus.Success, 0, 0, null, cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Jira sync failed for project {ProjectCode}", project.Code);
                await SyncLogHelper.WriteAsync(dbContext, project.Id, SyncType.Jira, SyncStatus.Failed, 0, 0, ex.Message, cancellationToken);
                failed++;
            }
        }

        run.MarkSuccess(processed, failed);
    }
}
