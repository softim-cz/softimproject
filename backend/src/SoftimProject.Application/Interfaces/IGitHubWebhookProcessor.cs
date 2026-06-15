namespace SoftimProject.Application.Interfaces;

// Parses and applies a GitHub webhook payload to the domain (issues, comments, PRs,
// commits, CI checks). Self-contained and idempotent at the data level (upserts by
// external id), so it can be driven both by the live webhook endpoint and by a
// dead-letter replay of a previously-failed delivery (#111).
//
// Signature verification and delivery-id idempotency are the caller's concern — by the
// time ProcessAsync runs, the payload is trusted.
public interface IGitHubWebhookProcessor
{
    Task<WebhookProcessResult> ProcessAsync(string eventType, string body, CancellationToken cancellationToken = default);
}

// RepositoryFound=false means no project is wired to the payload's repository (the live
// endpoint maps this to 404; replay treats it as a no-op success — the project is gone).
public sealed record WebhookProcessResult(bool RepositoryFound, string Message);
