using System.Text.Json;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Services;

// Replays a dead-lettered GitHub webhook (#111). The payload stores the original event
// type + raw body; we just re-drive the processor, which is idempotent (upserts by
// external id + the delivery-id ledger guards the endpoint). A vanished project is a
// no-op success — nothing left to apply.
public sealed class GitHubWebhookReplayHandler(IGitHubWebhookProcessor processor)
    : IDeadLetterReplayHandler
{
    public DeadLetterOperation OperationType => DeadLetterOperation.GitHubWebhook;

    public async Task<ReplayOutcome> ReplayAsync(DeadLetterEntry entry, CancellationToken cancellationToken = default)
    {
        StoredPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<StoredPayload>(entry.Payload);
        }
        catch (JsonException ex)
        {
            return new ReplayOutcome(false, $"Unreadable webhook payload: {ex.Message}");
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.EventType) || string.IsNullOrWhiteSpace(payload.Body))
            return new ReplayOutcome(false, "Webhook payload missing event type or body.");

        var result = await processor.ProcessAsync(payload.EventType, payload.Body, cancellationToken);
        return new ReplayOutcome(true, result.RepositoryFound ? null : "Repository no longer linked; nothing to apply.");
    }

    private sealed record StoredPayload(string EventType, string Body);
}
