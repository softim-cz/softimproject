using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using SoftimProject.Infrastructure.Services;

namespace SoftimProject.Infrastructure.Tests;

public class DataProtectionSecretProtectorTests
{
    private static DataProtectionSecretProtector Create()
        => new(new EphemeralDataProtectionProvider());

    [Fact]
    public void Protect_Then_Unprotect_RoundTrips()
    {
        var protector = Create();
        const string secret = "ep-api-token-123";

        var cipher = protector.Protect(secret);

        cipher.Should().NotBeNull();
        cipher.Should().NotBe(secret); // actually encrypted
        protector.Unprotect(cipher).Should().Be(secret);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Protect_And_Unprotect_PassThrough_NullOrEmpty(string? input)
    {
        var protector = Create();
        protector.Protect(input).Should().BeNull();
        protector.Unprotect(input).Should().BeNull();
    }

    [Fact]
    public void Unprotect_Foreign_Ciphertext_Throws()
    {
        // A value not produced by this protector (or a different key ring) must not
        // silently decrypt — Data Protection throws CryptographicException.
        var protector = Create();
        var act = () => protector.Unprotect("not-a-valid-protected-payload");
        act.Should().Throw<Exception>();
    }
}
