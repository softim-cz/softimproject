using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.BackgroundServices;

// Small per-project audit write used by the integration sync services (Jira, Redmine,
// GitHub). Separate from JobRun because SyncLog is a per-project history row (the
// GitHub incremental sync reads CompletedAt from here), whereas JobRun tracks one
// row per whole iteration for health reporting. Both coexist.
internal static class SyncLogHelper
{
    public static async Task WriteAsync(
        IApplicationDbContext dbContext,
        Guid projectId,
        SyncType syncType,
        SyncStatus status,
        int itemsSynced,
        int itemsFailed,
        string? error,
        CancellationToken cancellationToken)
    {
        dbContext.SyncLogs.Add(new SyncLog
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SyncType = syncType,
            Status = status,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            ItemsSynced = itemsSynced,
            ItemsFailed = itemsFailed,
            ErrorMessage = error,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
