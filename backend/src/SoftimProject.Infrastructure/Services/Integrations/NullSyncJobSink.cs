using SoftimProject.Application.Features.Migration.EasyProject.Dtos;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Services.Integrations;

/// <summary>
/// <see cref="ISyncJobSink"/> for headless scheduled syncs: no MigrationJob, no SignalR.
/// The caller (ExternalSyncService, milník 3c-ii) audits the run via SyncLog/JobRun and
/// reads the engine's in-memory progress (tracker) for counts.
/// </summary>
public sealed class NullSyncJobSink : ISyncJobSink
{
    public static readonly NullSyncJobSink Instance = new();

    public Task AdvancePhaseAsync(MigrationPhase phase, CancellationToken ct) => Task.CompletedTask;
    public Task CompleteAsync(bool hasErrors, MigrationProgressDto? progress, CancellationToken ct) => Task.CompletedTask;
    public Task CancelAsync(CancellationToken ct) => Task.CompletedTask;
    public Task FailAsync(string error, CancellationToken ct) => Task.CompletedTask;
    public Task NotifyAsync(MigrationProgressDto? progress) => Task.CompletedTask;
}
