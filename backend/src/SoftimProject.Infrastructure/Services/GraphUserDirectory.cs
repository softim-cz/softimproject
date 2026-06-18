using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Infrastructure.Services;

// Reads firemní role (jobTitle) and firma (companyName) for a user from Microsoft
// Graph using app-only credentials (same client-credentials pattern as the email
// integration). Requires the API's app registration to have the Graph application
// permission User.Read.All with admin consent.
//
// Any failure (missing consent, network, user not found) is swallowed and logged —
// the caller (provisioning) must keep working even when the directory is unreachable.
public sealed class GraphUserDirectory : IUserDirectory
{
    private readonly GraphServiceClient _graph;
    private readonly ILogger<GraphUserDirectory> _logger;

    public GraphUserDirectory(IConfiguration configuration, ILogger<GraphUserDirectory> logger)
    {
        _logger = logger;
        var credential = new ClientSecretCredential(
            configuration["AzureAd:TenantId"],
            configuration["AzureAd:ClientId"],
            configuration["AzureAd:ClientSecret"]);
        _graph = new GraphServiceClient(credential, scopes: ["https://graph.microsoft.com/.default"]);
    }

    public async Task<DirectoryUserProfile?> GetProfileAsync(
        string entraObjectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _graph.Users[entraObjectId].GetAsync(req =>
            {
                req.QueryParameters.Select = ["jobTitle", "companyName"];
            }, cancellationToken);

            if (user is null) return null;
            return new DirectoryUserProfile(user.JobTitle, user.CompanyName);
        }
        catch (Exception ex)
        {
            // Don't let an unreachable/misconfigured directory block user provisioning.
            _logger.LogWarning(ex, "Graph user lookup failed for {ObjectId}", entraObjectId);
            return null;
        }
    }
}

// Used when Graph is not configured (e.g. local dev). Returns no profile data so
// provisioning falls back to whatever the token carries.
public sealed class NullUserDirectory : IUserDirectory
{
    public Task<DirectoryUserProfile?> GetProfileAsync(
        string entraObjectId, CancellationToken cancellationToken = default)
        => Task.FromResult<DirectoryUserProfile?>(null);
}
