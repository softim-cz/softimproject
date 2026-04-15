using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Migration.EasyProject;

public sealed record FetchIssueCountsCommand(
    string BaseUrl,
    string ApiKey,
    string SessionId,
    List<int> ProjectIds) : IRequest;

public sealed class FetchIssueCountsCommandHandler(
    IServiceScopeFactory scopeFactory,
    ILoggerFactory loggerFactory) : IRequestHandler<FetchIssueCountsCommand>
{
    public Task Handle(FetchIssueCountsCommand request, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger<FetchIssueCountsCommandHandler>();

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var apiClient = scope.ServiceProvider.GetRequiredService<IEasyProjectApiClient>();
                var notifier = scope.ServiceProvider.GetRequiredService<IMigrationNotifier>();

                await Parallel.ForEachAsync(
                    request.ProjectIds,
                    new ParallelOptions { MaxDegreeOfParallelism = 5 },
                    async (projectId, ct) =>
                    {
                        try
                        {
                            var count = await apiClient.GetProjectIssueCountAsync(
                                request.BaseUrl, request.ApiKey, projectId, ct);
                            await notifier.SendIssueCountAsync(request.SessionId, projectId, count);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to fetch issue count for project {ProjectId}", projectId);
                            await notifier.SendIssueCountAsync(request.SessionId, projectId, 0);
                        }
                    });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch issue counts for session {SessionId}", request.SessionId);
            }
        });

        return Task.CompletedTask;
    }
}
