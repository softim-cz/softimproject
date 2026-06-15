namespace SoftimProject.Application.Interfaces;

/// <summary>
/// Result of provisioning a repo for integration. WebhookSecret is non-null only
/// when a NEW webhook was created (so the caller persists the new secret);
/// null means "left an existing webhook untouched".
/// </summary>
public sealed record GitHubProvisionResult(long? WebhookId, string? WebhookSecret, long? InstallationId);

/// <summary>
/// Sets up / tears down GitHub repo integration (webhook + App installation lookup)
/// when a project links/unlinks a repository.
/// </summary>
public interface IGitHubProvisioningService
{
    /// <summary>
    /// Ensures a webhook to our receiver exists on the repo (idempotent) and resolves
    /// the GitHub App installation id (if the App is configured). Uses the user's token
    /// for webhook management. No-ops gracefully when webhook URL is not configured.
    /// </summary>
    Task<GitHubProvisionResult> ProvisionRepoAsync(string owner, string repo, string userToken, CancellationToken cancellationToken = default);

    /// <summary>Best-effort removal of a previously-registered webhook.</summary>
    Task RemoveWebhookAsync(string owner, string repo, string userToken, long webhookId, CancellationToken cancellationToken = default);
}
