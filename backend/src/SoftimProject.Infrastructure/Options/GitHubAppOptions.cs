namespace SoftimProject.Infrastructure.Options;

/// <summary>
/// GitHub App credentials for server-to-server access (webhooks, sync) that is not
/// tied to a single user's OAuth token. When empty, the app falls back to the
/// existing user-OAuth / PAT flow, so the feature is opt-in.
/// </summary>
public sealed class GitHubAppOptions
{
    public const string SectionName = "GitHubApp";

    /// <summary>Numeric App ID (the "iss" of the app JWT).</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>PEM private key of the GitHub App (PKCS#1 or PKCS#8).</summary>
    public string PrivateKey { get; set; } = string.Empty;
}
