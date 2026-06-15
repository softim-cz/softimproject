using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using SoftimProject.Application.Interfaces;
using SoftimProject.Infrastructure.Options;

namespace SoftimProject.Infrastructure.Services;

/// <summary>
/// Generates GitHub App installation tokens. Signs a short-lived app JWT (RS256
/// with the App private key) and exchanges it via Octokit for an installation
/// access token, cached in-memory until shortly before expiry.
/// </summary>
public sealed class GitHubAppTokenService(
    IOptions<GitHubAppOptions> options,
    ILogger<GitHubAppTokenService> logger) : IGitHubAppTokenService
{
    private readonly GitHubAppOptions _options = options.Value;
    private readonly ConcurrentDictionary<long, (string Token, DateTimeOffset ExpiresAt)> _cache = new();

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.AppId) && !string.IsNullOrWhiteSpace(_options.PrivateKey);

    public async Task<string?> GetInstallationTokenAsync(long installationId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return null;

        if (_cache.TryGetValue(installationId, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            return cached.Token;

        try
        {
            var appJwt = CreateAppJwt(_options.AppId, _options.PrivateKey);
            var appClient = new GitHubClient(new ProductHeaderValue("SoftimProject"))
            {
                Credentials = new Credentials(appJwt, AuthenticationType.Bearer),
            };

            var token = await appClient.GitHubApps.CreateInstallationToken(installationId);
            _cache[installationId] = (token.Token, token.ExpiresAt);
            return token.Token;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to mint GitHub App installation token for installation {InstallationId}", installationId);
            return null;
        }
    }

    internal static string CreateAppJwt(string appId, string privateKeyPem)
    {
        var now = DateTimeOffset.UtcNow;
        var header = new { alg = "RS256", typ = "JWT" };
        var payload = new
        {
            iat = now.AddSeconds(-60).ToUnixTimeSeconds(), // clock-skew cushion
            exp = now.AddMinutes(9).ToUnixTimeSeconds(),   // GitHub max is 10 minutes
            iss = appId,
        };

        var encodedHeader = Base64Url(JsonSerializer.SerializeToUtf8Bytes(header));
        var encodedPayload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload));
        var signingInput = $"{encodedHeader}.{encodedPayload}";

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return $"{signingInput}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
