using System.Text.Json;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Integrations;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Services.Integrations;

/// <summary>Outcome of one connection's incremental sync run.</summary>
public sealed record ExternalSyncOutcome(int Synced, int Failed, bool HardFailed, string? Error)
{
    public static ExternalSyncOutcome Fail(string error) => new(0, 0, true, error);
}

/// <summary>
/// Runs one <see cref="IntegrationConnection"/>'s incremental sync: decrypts the token,
/// rebuilds a <see cref="SyncEngineRequest"/> from the persisted mappings/options, runs the
/// provider-agnostic <see cref="SyncEngine"/> headless (<see cref="NullSyncJobSink"/>) with
/// <c>ChangedSince = LastSyncWatermark</c>, and advances the watermark on success. Kept apart
/// from the hosted service so it is unit-testable.
/// </summary>
public sealed class ExternalSyncRunner(
    IApplicationDbContext dbContext,
    ISecretProtector protector,
    SyncEngine syncEngine,
    IEnumerable<ISourceConnector> connectors,
    IMigrationProgressTracker tracker,
    ILogger<ExternalSyncRunner> logger)
{
    public async Task<ExternalSyncOutcome> RunAsync(IntegrationConnection connection, CancellationToken ct)
    {
        if (connection.CreatedByUserId == Guid.Empty)
            return ExternalSyncOutcome.Fail("Connection has no CreatedByUserId to attribute the sync to.");

        var token = protector.Unprotect(connection.EncryptedApiToken);
        if (string.IsNullOrEmpty(token))
            return ExternalSyncOutcome.Fail("Connection has no usable API token.");

        var connector = connectors.FirstOrDefault(c => c.SourceSystem == connection.SourceSystem);
        if (connector is null)
            return ExternalSyncOutcome.Fail($"No connector registered for {connection.SourceSystem}.");

        SyncEngineRequest request;
        try
        {
            request = BuildRequest(connection);
        }
        catch (Exception ex)
        {
            return ExternalSyncOutcome.Fail($"Stored connection config is invalid: {ex.Message}");
        }

        // Watermark captured at the start; on success the next run pulls everything changed
        // since now. Slight overlap is harmless thanks to idempotent ExternalId upserts.
        var watermarkCandidate = DateTime.UtcNow;
        var runId = Guid.NewGuid();
        tracker.Init(runId);

        await syncEngine.ExecuteAsync(
            runId,
            connection.CreatedByUserId,
            request,
            connector,
            new SourceConnectionContext(connection.BaseUrl, token),
            NullSyncJobSink.Instance);

        var progress = tracker.GetProgress(runId);
        if (progress?.Status == "Failed")
        {
            var error = progress.RecentErrors.LastOrDefault() ?? "Sync failed.";
            logger.LogError("Incremental sync failed for connection {ConnectionId}: {Error}", connection.Id, error);
            return ExternalSyncOutcome.Fail(error);
        }

        connection.LastSyncWatermark = watermarkCandidate;
        await dbContext.SaveChangesAsync(ct);

        var synced = (progress?.ItemsCreated ?? 0) + (progress?.ItemsUpdated ?? 0);
        var failed = progress?.ErrorCount ?? 0;
        logger.LogInformation("Incremental sync done for connection {ConnectionId}: {Synced} synced, {Failed} failed", connection.Id, synced, failed);
        return new ExternalSyncOutcome(synced, failed, HardFailed: false, Error: null);
    }

    private SyncEngineRequest BuildRequest(IntegrationConnection connection)
    {
        var mappings = Deserialize<StoredConnectionMappings>(connection.MappingsJson)
            ?? throw new InvalidOperationException("MappingsJson missing.");
        var options = Deserialize<StoredConnectionOptions>(connection.OptionsJson)
            ?? throw new InvalidOperationException("OptionsJson missing.");
        var projectIds = Deserialize<List<string>>(connection.ProjectSelectorJson) ?? [];

        return new SyncEngineRequest(
            connection.TargetProjectTemplateId,
            projectIds,
            mappings.TrackerMapping,
            mappings.StatusMapping,
            mappings.PriorityMapping,
            mappings.UserMapping,
            options.SkipClosedIssues,
            options.SkipAttachments,
            options.ImportComments,
            options.ImportWorklogs,
            options.ImportChecklists,
            options.CreateMissingUsers,
            mappings.AutoCreateTrackers,
            mappings.AutoCreateStatuses,
            mappings.AutoCreateStatusIsClosed,
            mappings.AutoCreatePriorities,
            ChangedSince: connection.LastSyncWatermark,
            IntegrationConnectionId: connection.Id,
            TargetCompanyId: connection.TargetCompanyId,
            ConflictPolicy: connection.ConflictPolicy);
    }

    private static T? Deserialize<T>(string? json) =>
        string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json);
}
