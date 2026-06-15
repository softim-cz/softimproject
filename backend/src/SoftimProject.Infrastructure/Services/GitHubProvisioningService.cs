using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using SoftimProject.Application.Interfaces;
using SoftimProject.Infrastructure.Options;

namespace SoftimProject.Infrastructure.Services;

public sealed class GitHubProvisioningService(
    IOptions<GitHubOptions> gitHubOptions,
    IGitHubAppTokenService appTokenService,
    ILogger<GitHubProvisioningService> logger) : IGitHubProvisioningService
{
    private static readonly string[] WebhookEvents = ["push", "issues", "issue_comment", "pull_request"];
    private readonly GitHubOptions _options = gitHubOptions.Value;

    public async Task<GitHubProvisionResult> ProvisionRepoAsync(string owner, string repo, string userToken, CancellationToken cancellationToken = default)
    {
        long? webhookId = null;
        string? webhookSecret = null;

        if (!string.IsNullOrWhiteSpace(_options.WebhookUrl))
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("SoftimProject"))
                {
                    Credentials = new Credentials(userToken),
                };

                var existing = await client.Repository.Hooks.GetAll(owner, repo);
                var ours = existing.FirstOrDefault(h =>
                    h.Config != null
                    && h.Config.TryGetValue("url", out var url)
                    && string.Equals(url, _options.WebhookUrl, StringComparison.OrdinalIgnoreCase));

                if (ours != null)
                {
                    // Leave the existing hook (and its secret) in place.
                    webhookId = ours.Id;
                }
                else
                {
                    webhookSecret = GenerateSecret();
                    var config = new Dictionary<string, string>
                    {
                        ["url"] = _options.WebhookUrl,
                        ["content_type"] = "json",
                        ["secret"] = webhookSecret,
                        ["insecure_ssl"] = "0",
                    };
                    var created = await client.Repository.Hooks.Create(owner, repo,
                        new NewRepositoryHook("web", config) { Events = WebhookEvents, Active = true });
                    webhookId = created.Id;
                }
            }
            catch (Exception ex)
            {
                // Don't fail the link if webhook setup fails (e.g. missing admin:repo_hook scope).
                logger.LogWarning(ex, "Auto-registration of GitHub webhook for {Owner}/{Repo} failed", owner, repo);
            }
        }

        long? installationId = await appTokenService.GetRepositoryInstallationIdAsync(owner, repo, cancellationToken);

        return new GitHubProvisionResult(webhookId, webhookSecret, installationId);
    }

    public async Task RemoveWebhookAsync(string owner, string repo, string userToken, long webhookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = new GitHubClient(new ProductHeaderValue("SoftimProject"))
            {
                Credentials = new Credentials(userToken),
            };
            await client.Repository.Hooks.Delete(owner, repo, (int)webhookId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Removing GitHub webhook {WebhookId} for {Owner}/{Repo} failed", webhookId, owner, repo);
        }
    }

    private static string GenerateSecret() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
}
