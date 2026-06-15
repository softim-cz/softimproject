using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SoftimProject.Infrastructure.Options;
using SoftimProject.Infrastructure.Services;
using Xunit;

namespace SoftimProject.Infrastructure.Tests;

public class GitHubAppTokenServiceTests
{
    [Fact]
    public void IsConfigured_false_when_empty()
    {
        var svc = Create(new GitHubAppOptions());
        svc.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task GetInstallationToken_returns_null_when_not_configured()
    {
        var svc = Create(new GitHubAppOptions());
        var token = await svc.GetInstallationTokenAsync(123, CancellationToken.None);
        token.Should().BeNull();
    }

    [Fact]
    public void IsConfigured_true_when_appid_and_key_present()
    {
        using var rsa = RSA.Create(2048);
        var pem = rsa.ExportRSAPrivateKeyPem();
        var svc = Create(new GitHubAppOptions { AppId = "12345", PrivateKey = pem });
        svc.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void CreateAppJwt_produces_valid_rs256_token()
    {
        using var rsa = RSA.Create(2048);
        var pem = rsa.ExportRSAPrivateKeyPem();

        var jwt = GitHubAppTokenService.CreateAppJwt("999", pem);

        var parts = jwt.Split('.');
        parts.Should().HaveCount(3);

        // Header: RS256/JWT
        var header = JsonSerializer.Deserialize<JsonElement>(Decode(parts[0]));
        header.GetProperty("alg").GetString().Should().Be("RS256");

        // Payload: issuer is the app id, exp after iat
        var payload = JsonSerializer.Deserialize<JsonElement>(Decode(parts[1]));
        payload.GetProperty("iss").GetString().Should().Be("999");
        payload.GetProperty("exp").GetInt64().Should().BeGreaterThan(payload.GetProperty("iat").GetInt64());

        // Signature verifies against the public key
        var signingInput = Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}");
        var signature = Decode(parts[2]);
        rsa.VerifyData(signingInput, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .Should().BeTrue();
    }

    private static GitHubAppTokenService Create(GitHubAppOptions opts) =>
        new(Microsoft.Extensions.Options.Options.Create(opts), NullLogger<GitHubAppTokenService>.Instance);

    private static byte[] Decode(string segment)
    {
        var s = segment.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
