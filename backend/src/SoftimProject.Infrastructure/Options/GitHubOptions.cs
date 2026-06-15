namespace SoftimProject.Infrastructure.Options;

public sealed class GitHubOptions
{
    public const string SectionName = "GitHub";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Public URL of the GitHub webhook receiver (e.g.
    /// https://softimproject-api.azurewebsites.net/api/webhooks/github). When empty,
    /// webhooks are not auto-registered on repo link (e.g. local dev without a public URL).
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;
}
