using Microsoft.AspNetCore.SignalR;
using SoftimProject.Application.Features.Migration.EasyProject.Dtos;
using SoftimProject.Application.Interfaces;
using SoftimProject.WebApi.Hubs;

namespace SoftimProject.WebApi.Services;

public sealed class MigrationNotifier(IHubContext<MigrationHub> hubContext) : IMigrationNotifier
{
    public async Task NotifyProgressAsync(Guid jobId, MigrationProgressDto progress)
    {
        await hubContext.Clients.Group($"migration-{jobId}")
            .SendAsync("MigrationProgress", progress);
    }

    public async Task SendFetchProgressAsync(string sessionId, string message, int current, int total)
    {
        await hubContext.Clients.Group($"fetch-{sessionId}")
            .SendAsync("FetchProgress", new { message, current, total });
    }

    public async Task SendIssueCountAsync(string sessionId, int epId, int issueCount)
    {
        await hubContext.Clients.Group($"fetch-{sessionId}")
            .SendAsync("ProjectIssueCount", new { epId, issueCount });
    }
}
