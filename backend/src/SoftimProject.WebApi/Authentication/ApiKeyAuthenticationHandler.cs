using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.WebApi.Authentication;

/// <summary>
/// Authenticates requests carrying a personal API key (`Authorization: Bearer spk_…`
/// or `X-Api-Key: spk_…`). Sets the same `oid` claim a real Entra token would, so
/// CurrentUserService resolves the owning user and all permissions apply unchanged.
/// Returns NoResult when no API key is present, so JWT/Dev schemes can still handle
/// the request.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApplicationDbContext dbContext)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var key = ExtractKey();
        if (key is null)
            return AuthenticateResult.NoResult();

        var hash = ApiKeyHasher.Hash(key);
        var now = DateTime.UtcNow;

        var record = await dbContext.ApiKeys
            .Where(k => k.KeyHash == hash)
            .Select(k => new { k.Id, k.RevokedAt, k.ExpiresAt, k.LastUsedAt, Oid = k.User.EntraObjectId, k.User.IsActive })
            .FirstOrDefaultAsync();

        if (record is null)
            return AuthenticateResult.Fail("Invalid API key.");
        if (record.RevokedAt is not null)
            return AuthenticateResult.Fail("API key has been revoked.");
        if (record.ExpiresAt is not null && record.ExpiresAt < now)
            return AuthenticateResult.Fail("API key has expired.");
        if (!record.IsActive)
            return AuthenticateResult.Fail("User is inactive.");

        // Best-effort, throttled last-used stamp (never block auth on it).
        if (record.LastUsedAt is null || now - record.LastUsedAt.Value > TimeSpan.FromMinutes(5))
        {
            try
            {
                await dbContext.ApiKeys
                    .Where(k => k.Id == record.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, now));
            }
            catch
            {
                // ignore — usage stamp is not critical
            }
        }

        var claims = new[]
        {
            new Claim("oid", record.Oid),
            new Claim(ClaimTypes.Name, record.Oid),
            new Claim("auth_method", "api_key"),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return AuthenticateResult.Success(ticket);
    }

    private string? ExtractKey()
    {
        var auth = Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = auth["Bearer ".Length..].Trim();
            if (token.StartsWith(ApiKeyHasher.Prefix, StringComparison.Ordinal))
                return token;
        }

        var headerKey = Request.Headers["X-Api-Key"].ToString();
        if (!string.IsNullOrEmpty(headerKey) && headerKey.StartsWith(ApiKeyHasher.Prefix, StringComparison.Ordinal))
            return headerKey;

        return null;
    }
}
