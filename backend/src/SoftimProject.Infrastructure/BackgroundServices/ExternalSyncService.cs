using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Services.Integrations;

namespace SoftimProject.Infrastructure.BackgroundServices;

/// <summary>
/// Recurring incremental sync for enabled <c>IntegrationConnection</c>s (the automation core
/// of #144). Ticks every 15 min and runs each connection whose interval has elapsed; the
/// per-connection work (delta pull, headless SyncEngine, watermark) lives in
/// <see cref="ExternalSyncRunner"/>. Failures are dead-lettered for admin visibility.
/// </summary>
public sealed class ExternalSyncService(
    IServiceScopeFactory scopeFactory,
    IJobRegistry jobRegistry,
    ILogger<ExternalSyncService> logger)
    : TrackedBackgroundService(scopeFactory, jobRegistry, logger, TimeSpan.FromMinutes(15))
{
    protected override async Task ExecuteIterationAsync(
        IServiceProvider services,
        IJobRunScope run,
        CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<IApplicationDbContext>();
        var runner = services.GetRequiredService<ExternalSyncRunner>();
        var deadLetters = services.GetRequiredService<IDeadLetterQueue>();

        var connections = await dbContext.IntegrationConnections
            .Where(c => c.IsEnabled && c.Mode != IntegrationSyncMode.Manual)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var processed = 0;
        var failed = 0;

        foreach (var connection in connections)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var due = connection.LastSyncStartedAt is null
                || now - connection.LastSyncStartedAt.Value >= TimeSpan.FromMinutes(connection.IntervalMinutes);
            if (!due) continue;

            // Claim the run up front so a long sync isn't re-picked on the next tick.
            connection.LastSyncStartedAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);

            ExternalSyncOutcome outcome;
            try
            {
                outcome = await runner.RunAsync(connection, cancellationToken);
            }
            catch (Exception ex)
            {
                outcome = ExternalSyncOutcome.Fail(ex.Message);
            }

            if (outcome.HardFailed)
            {
                logger.LogError("Incremental sync failed for connection {ConnectionId}: {Error}", connection.Id, outcome.Error);
                var payload = JsonSerializer.Serialize(new { connectionId = connection.Id, connection.SourceSystem, connection.BaseUrl });
                await deadLetters.EnqueueAsync(
                    DeadLetterOperation.ExternalSync,
                    connection.Id.ToString(),
                    payload,
                    new InvalidOperationException(outcome.Error ?? "Sync failed."),
                    cancellationToken);
                failed++;
            }
            else
            {
                processed++;
            }
        }

        run.MarkSuccess(processed, failed);
    }
}
