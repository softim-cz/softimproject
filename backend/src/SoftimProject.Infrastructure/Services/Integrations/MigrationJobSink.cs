using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Features.Migration.EasyProject.Dtos;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Services.Integrations;

/// <summary>
/// <see cref="ISyncJobSink"/> for the wizard-driven one-time migration: persists run state
/// onto the <c>MigrationJob</c> row and pushes progress over SignalR. Reproduces exactly
/// what the engine used to do inline, so migration behavior is unchanged.
/// </summary>
public sealed class MigrationJobSink(IApplicationDbContext dbContext, IMigrationNotifier notifier, Guid jobId) : ISyncJobSink
{
    public async Task AdvancePhaseAsync(MigrationPhase phase, CancellationToken ct)
    {
        var job = await dbContext.MigrationJobs.FindAsync([jobId], ct);
        if (job is null) return;
        if (phase > job.CurrentPhase)
        {
            job.CurrentPhase = phase;
            await dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task CompleteAsync(bool hasErrors, MigrationProgressDto? progress, CancellationToken ct)
    {
        var job = await dbContext.MigrationJobs.FindAsync([jobId], ct);
        if (job is null) return;
        job.Status = hasErrors ? MigrationStatus.CompletedWithErrors : MigrationStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;
        job.CurrentPhase = MigrationPhase.Done;
        job.ProjectsMigrated = progress?.ProjectsMigrated ?? 0;
        job.TicketsMigrated = progress?.TicketsMigrated ?? 0;
        job.ItemsFailed = progress?.ErrorCount ?? 0;
        job.ItemsCreated = progress?.ItemsCreated ?? 0;
        job.ItemsUpdated = progress?.ItemsUpdated ?? 0;
        job.ItemsSkipped = progress?.ItemsSkipped ?? 0;
        job.ErrorLog = progress?.RecentErrors.Count > 0 ? JsonSerializer.Serialize(progress.RecentErrors) : null;
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(CancellationToken ct)
    {
        var job = await dbContext.MigrationJobs.FindAsync([jobId], ct);
        if (job != null) { job.Status = MigrationStatus.Cancelled; job.CompletedAt = DateTime.UtcNow; }
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task FailAsync(string error, CancellationToken ct)
    {
        var job = await dbContext.MigrationJobs.FindAsync([jobId], ct);
        if (job != null) { job.Status = MigrationStatus.Failed; job.CompletedAt = DateTime.UtcNow; job.ErrorLog = JsonSerializer.Serialize(new[] { error }); }
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task NotifyAsync(MigrationProgressDto? progress)
    {
        if (progress != null) await notifier.NotifyProgressAsync(jobId, progress);
    }
}
