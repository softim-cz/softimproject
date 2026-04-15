using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Octokit;
using SoftimProject.Application.Interfaces;
using SoftimProject.Infrastructure.Options;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace SoftimProject.WebApi.Controllers;

public class GitHubOAuthController : ApiControllerBase
{
    [HttpGet("authorize")]
    public IActionResult Authorize(
        [FromQuery] Guid projectId,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IOptions<GitHubOptions> options,
        [FromServices] IDataProtectionProvider dataProtection)
    {
        if (!currentUser.UserId.HasValue)
            return Unauthorized();

        var gitHub = options.Value;
        if (string.IsNullOrEmpty(gitHub.ClientId))
            return BadRequest(new { error = "GitHub OAuth is not configured" });

        var protector = dataProtection.CreateProtector("GitHubOAuth");
        var state = protector.Protect(JsonSerializer.Serialize(new OAuthState(
            currentUser.UserId.Value,
            projectId,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds())));

        var url = $"https://github.com/login/oauth/authorize?client_id={gitHub.ClientId}&redirect_uri={Uri.EscapeDataString(gitHub.CallbackUrl)}&scope=repo&state={Uri.EscapeDataString(state)}";

        return Ok(new { url });
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        [FromServices] IOptions<GitHubOptions> options,
        [FromServices] IDataProtectionProvider dataProtection,
        [FromServices] IHttpClientFactory httpClientFactory,
        [FromServices] IApplicationDbContext dbContext,
        [FromServices] IConfiguration configuration,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return BadRequest("Missing code or state");

        var gitHub = options.Value;
        var frontendUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";

        // Decrypt and validate state
        OAuthState oauthState;
        try
        {
            var protector = dataProtection.CreateProtector("GitHubOAuth");
            var json = protector.Unprotect(state);
            oauthState = JsonSerializer.Deserialize<OAuthState>(json)!;
        }
        catch
        {
            return BadRequest("Invalid or expired state parameter");
        }

        // Check timestamp (max 10 minutes)
        var elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - oauthState.Timestamp;
        if (elapsed > 600)
            return BadRequest("OAuth state expired");

        // Exchange code for access token
        var httpClient = httpClientFactory.CreateClient();
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");
        tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        tokenRequest.Content = JsonContent.Create(new
        {
            client_id = gitHub.ClientId,
            client_secret = gitHub.ClientSecret,
            code,
            redirect_uri = gitHub.CallbackUrl
        });

        var tokenResponse = await httpClient.SendAsync(tokenRequest, ct);
        var tokenBody = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(ct);

        if (!tokenBody.TryGetProperty("access_token", out var accessTokenEl))
        {
            var error = tokenBody.TryGetProperty("error_description", out var errDesc)
                ? errDesc.GetString()
                : "Failed to exchange code for token";
            return Redirect($"{frontendUrl}/projects/{oauthState.ProjectId}/settings?github=error&message={Uri.EscapeDataString(error ?? "Unknown error")}");
        }

        var accessToken = accessTokenEl.GetString()!;

        // Get GitHub user info
        var ghClient = new GitHubClient(new ProductHeaderValue("SoftimProject"))
        {
            Credentials = new Credentials(accessToken)
        };
        var ghUser = await ghClient.User.Current();

        // Save token and login to user
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == oauthState.UserId, ct);
        if (user == null)
            return BadRequest("User not found");

        user.GitHubAccessToken = accessToken;
        user.GitHubLogin = ghUser.Login;
        await dbContext.SaveChangesAsync(ct);

        return Redirect($"{frontendUrl}/projects/{oauthState.ProjectId}/settings?github=connected");
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IApplicationDbContext dbContext,
        CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            return Unauthorized();

        var user = await dbContext.Users
            .Where(u => u.Id == currentUser.UserId.Value)
            .Select(u => new { u.GitHubAccessToken, u.GitHubLogin })
            .FirstOrDefaultAsync(ct);

        var connected = !string.IsNullOrEmpty(user?.GitHubAccessToken);
        return Ok(new { connected, login = connected ? user!.GitHubLogin : null });
    }

    [HttpGet("repos")]
    public async Task<IActionResult> Repos(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IApplicationDbContext dbContext,
        CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            return Unauthorized();

        var token = await dbContext.Users
            .Where(u => u.Id == currentUser.UserId.Value)
            .Select(u => u.GitHubAccessToken)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrEmpty(token))
            return BadRequest(new { error = "GitHub not connected" });

        var client = new GitHubClient(new ProductHeaderValue("SoftimProject"))
        {
            Credentials = new Credentials(token)
        };

        var repos = await client.Repository.GetAllForCurrent(new RepositoryRequest
        {
            Sort = RepositorySort.Updated,
            Direction = SortDirection.Descending
        });

        var result = repos.Select(r => new
        {
            fullName = r.FullName,
            description = r.Description,
            isPrivate = r.Private
        }).ToList();

        return Ok(result);
    }

    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] IApplicationDbContext dbContext,
        CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            return Unauthorized();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId.Value, ct);
        if (user == null)
            return NotFound();

        user.GitHubAccessToken = null;
        user.GitHubLogin = null;

        // Clear GitHubConnectedByUserId from any projects connected by this user
        var projects = await dbContext.Projects
            .Where(p => p.GitHubConnectedByUserId == currentUser.UserId.Value)
            .ToListAsync(ct);

        foreach (var project in projects)
            project.GitHubConnectedByUserId = null;

        await dbContext.SaveChangesAsync(ct);
        return NoContent();
    }

    private sealed record OAuthState(Guid UserId, Guid ProjectId, long Timestamp);
}
