using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.Services.Integrations;

public sealed class IntegrationSyncTrigger(IApplicationDbContext dbContext, ExternalSyncRunner runner)
    : IIntegrationSyncTrigger
{
    public async Task RunNowAsync(Guid connectionId, CancellationToken ct)
    {
        var connection = await dbContext.IntegrationConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId, ct);
        if (connection is null) return;

        connection.LastSyncStartedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(ct);
        await runner.RunAsync(connection, ct);
    }
}
