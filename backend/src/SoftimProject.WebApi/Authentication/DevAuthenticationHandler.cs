using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SoftimProject.WebApi.Authentication;

/// <summary>
/// Dev-only auth. Reads X-Dev-User-Id header (EntraObjectId of a seeded
/// user) and builds a ClaimsPrincipal so CurrentUserService picks it up
/// the same way it would for a real Entra token. Falls back to
/// DevAuth:DefaultUserId from config when no header is present.
/// Must never be registered outside IsDevelopment.
/// </summary>
public sealed class DevAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Dev";
    private const string UserIdHeader = "X-Dev-User-Id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var entraObjectId = Request.Headers[UserIdHeader].ToString();
        if (string.IsNullOrWhiteSpace(entraObjectId))
        {
            entraObjectId = configuration["DevAuth:DefaultUserId"] ?? "";
        }

        if (string.IsNullOrWhiteSpace(entraObjectId))
        {
            return Task.FromResult(AuthenticateResult.Fail(
                $"Missing {UserIdHeader} header and no DevAuth:DefaultUserId configured."));
        }

        var claims = new[]
        {
            new Claim("oid", entraObjectId),
            new Claim(ClaimTypes.Name, entraObjectId),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
