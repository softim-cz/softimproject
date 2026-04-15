using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Services;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class GitHubSyncService(IServiceScopeFactory scopeFactory, ILogger<GitHubSyncService> logger)
    : SyncBackgroundServiceBase(scopeFactory, logger, TimeSpan.FromMinutes(5), SyncType.GitHub)
{
    protected override async Task ExecuteSyncAsync(IServiceProvider services, IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var projects = await dbContext.Projects
            .Where(p => p.ExternalSystem == "GitHub" && p.ExternalProjectId != null && p.Status == ProjectStatus.Active)
            .ToListAsync(cancellationToken);

        // Resolve default TaskState and TicketPriority IDs once
        var defaultStateId = await dbContext.TaskStates
            .Where(ts => ts.IsActive && ts.IsDefault)
            .Select(ts => ts.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (defaultStateId == Guid.Empty)
            defaultStateId = await dbContext.TaskStates.Where(ts => ts.IsActive).OrderBy(ts => ts.SortOrder).Select(ts => ts.Id).FirstAsync(cancellationToken);

        var closedStateId = await dbContext.TaskStates
            .Where(ts => ts.IsActive && ts.IsClosedState)
            .Select(ts => ts.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (closedStateId == Guid.Empty)
            closedStateId = defaultStateId;

        var defaultPriorityId = await dbContext.TicketPriorities
            .Where(tp => tp.IsActive && tp.IsDefault)
            .Select(tp => tp.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (defaultPriorityId == Guid.Empty)
            defaultPriorityId = await dbContext.TicketPriorities.Where(tp => tp.IsActive).OrderBy(tp => tp.SortOrder).Select(tp => tp.Id).FirstAsync(cancellationToken);

        foreach (var project in projects)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(project.ExternalProjectId))
                    continue;

                string? token = project.ExternalApiToken;
                if (project.GitHubConnectedByUserId.HasValue)
                {
                    token = await dbContext.Users
                        .Where(u => u.Id == project.GitHubConnectedByUserId.Value)
                        .Select(u => u.GitHubAccessToken)
                        .FirstOrDefaultAsync(cancellationToken);
                }
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                var parts = project.ExternalProjectId.Split('/');
                if (parts.Length != 2) continue;

                var owner = parts[0];
                var repo = parts[1];

                var client = new GitHubClient(new ProductHeaderValue("SoftimProject"))
                {
                    Credentials = new Credentials(token)
                };

                var lastSync = await dbContext.SyncLogs
                    .Where(s => s.ProjectId == project.Id && s.SyncType == SyncType.GitHub && s.Status == SyncStatus.Success)
                    .OrderByDescending(s => s.CompletedAt)
                    .Select(s => s.CompletedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                var (synced, failed) = await GitHubSyncHelper.SyncAsync(
                    client, owner, repo, project, dbContext, lastSync,
                    defaultStateId, closedStateId, defaultPriorityId,
                    logger, cancellationToken);

                logger.LogInformation("GitHub sync completed for project {ProjectCode}: {Synced} synced, {Failed} failed", project.Code, synced, failed);
                await LogSyncAsync(dbContext, project.Id, SyncStatus.Success, synced, failed, null, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GitHub sync failed for project {ProjectCode}", project.Code);
                await LogSyncAsync(dbContext, project.Id, SyncStatus.Failed, 0, 0, ex.Message, cancellationToken);
            }
        }
    }
}
