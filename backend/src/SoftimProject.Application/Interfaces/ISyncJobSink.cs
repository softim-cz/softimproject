using SoftimProject.Application.Features.Migration.EasyProject.Dtos;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Interfaces;

/// <summary>
/// Reporting seam for a SyncEngine run. Decouples the engine from the MigrationJob /
/// SignalR machinery so it can run both as a wizard-driven migration (MigrationJobSink)
/// and as a headless scheduled sync (NullSyncJobSink — audited via SyncLog/JobRun instead).
/// </summary>
public interface ISyncJobSink
{
    /// <summary>Records that a phase boundary was reached (migration persists it; headless ignores).</summary>
    Task AdvancePhaseAsync(MigrationPhase phase, CancellationToken ct);

    /// <summary>Finalizes the run as completed (with or without item-level errors).</summary>
    Task CompleteAsync(bool hasErrors, MigrationProgressDto? progress, CancellationToken ct);

    /// <summary>Finalizes the run as cancelled.</summary>
    Task CancelAsync(CancellationToken ct);

    /// <summary>Finalizes the run as failed.</summary>
    Task FailAsync(string error, CancellationToken ct);

    /// <summary>Pushes a progress snapshot to listeners (migration → SignalR; headless → no-op).</summary>
    Task NotifyAsync(MigrationProgressDto? progress);
}
