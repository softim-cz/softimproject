using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using Polly.Registry;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Services;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class GitHubSyncService(
    IServiceScopeFactory scopeFactory,
    IJobRegistry jobRegistry,
    ILogger<GitHubSyncService> logger)
    : TrackedBackgroundService(scopeFactory, jobRegistry, logger, TimeSpan.FromMinutes(5))
{
    protected override async Task ExecuteIterationAsync(
        IServiceProvider services,
        IJobRunScope run,
        CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<IApplicationDbContext>();
        var pipeline = services.GetRequiredService<ResiliencePipelineProvider<string>>()
            .GetPipeline(ResiliencePipelines.GitHubApi);
        var deadLetters = services.GetRequiredService<IDeadLetterQueue>();

        var projects = await dbContext.Projects
            .Where(p => p.ExternalSystem == "GitHub" && p.ExternalProjectId != null && p.Status == ProjectStatus.Active)
            .ToListAsync(cancellationToken);

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

        var processedProjects = 0;
        var failedProjects = 0;
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

                // Wrap the whole per-project helper in the shared retry pipeline. Octokit
                // itself has no backoff, so a transient 502/504 or rate-limit burst would
                // otherwise dead-letter immediately.
                var (synced, failed) = await pipeline.ExecuteAsync(
                    async ct => await GitHubSyncHelper.SyncAsync(
                        client, owner, repo, project, dbContext, lastSync,
                        defaultStateId, closedStateId, defaultPriorityId,
                        logger, ct),
                    cancellationToken);

                logger.LogInformation("GitHub sync completed for project {ProjectCode}: {Synced} synced, {Failed} failed", project.Code, synced, failed);
                await SyncLogHelper.WriteAsync(dbContext, project.Id, SyncType.GitHub, SyncStatus.Success, synced, failed, null, cancellationToken);
                processedProjects++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GitHub sync failed for project {ProjectCode} after retries", project.Code);
                await SyncLogHelper.WriteAsync(dbContext, project.Id, SyncType.GitHub, SyncStatus.Failed, 0, 0, ex.Message, cancellationToken);

                var payload = JsonSerializer.Serialize(new
                {
                    projectId = project.Id,
                    projectCode = project.Code,
                    externalProjectId = project.ExternalProjectId,
                });
                await deadLetters.EnqueueAsync(
                    DeadLetterOperation.GitHubSyncProject,
                    project.Id.ToString(),
                    payload,
                    ex,
                    cancellationToken);
                failedProjects++;
            }
        }

        run.MarkSuccess(processedProjects, failedProjects);
    }
}
