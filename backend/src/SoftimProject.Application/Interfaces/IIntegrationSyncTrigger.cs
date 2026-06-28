namespace SoftimProject.Application.Interfaces;

/// <summary>
/// Triggers an immediate incremental sync of a single connection (used by the webhook
/// receiver for near-real-time updates). The delta pull figures out what changed, so no
/// provider-specific payload parsing is needed.
/// </summary>
public interface IIntegrationSyncTrigger
{
    Task RunNowAsync(Guid connectionId, CancellationToken ct);
}
