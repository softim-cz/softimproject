namespace SoftimProject.Domain.Entities;

// Idempotency ledger for inbound webhooks (#111). One row per provider delivery id
// (GitHub's X-GitHub-Delivery). A delivery whose id is already recorded is skipped,
// so GitHub's automatic redelivery / our own DLQ replay can't double-apply an event.
public class ProcessedWebhookDelivery
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = "GitHub";
    public string DeliveryId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
}
