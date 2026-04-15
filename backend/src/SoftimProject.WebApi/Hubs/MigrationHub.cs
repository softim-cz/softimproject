using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SoftimProject.WebApi.Hubs;

[Authorize]
public class MigrationHub : Hub
{
    public async Task JoinMigrationJob(string jobId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"migration-{jobId}");

    public async Task LeaveMigrationJob(string jobId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"migration-{jobId}");

    public async Task JoinFetchSession(string sessionId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"fetch-{sessionId}");

    public async Task LeaveFetchSession(string sessionId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"fetch-{sessionId}");
}
