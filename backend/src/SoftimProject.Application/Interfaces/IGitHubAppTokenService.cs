namespace SoftimProject.Application.Interfaces;

/// <summary>
/// Mints short-lived GitHub App installation access tokens for server-to-server
/// calls. Falls back to user OAuth / PAT when the GitHub App is not configured.
/// </summary>
public interface IGitHubAppTokenService
{
    /// <summary>True when a GitHub App (AppId + private key) is configured.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Returns a cached or freshly-minted installation access token for the given
    /// installation, or null when the app is not configured / minting fails.
    /// </summary>
    Task<string?> GetInstallationTokenAsync(long installationId, CancellationToken cancellationToken = default);
}
