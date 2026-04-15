using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Projects.GitHub;

public sealed record TestGitHubConnectionQuery(Guid ProjectId) : IRequest<TestGitHubConnectionResult>, IRequireProjectAccess;

public sealed record TestGitHubConnectionResult(bool Success, string? Error, string? RepositoryName);

public sealed class TestGitHubConnectionQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<TestGitHubConnectionQuery, TestGitHubConnectionResult>
{
    public async Task<TestGitHubConnectionResult> Handle(TestGitHubConnectionQuery request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new Common.NotFoundException(nameof(Domain.Entities.Project), request.ProjectId);

        if (string.IsNullOrWhiteSpace(project.ExternalProjectId))
            return new TestGitHubConnectionResult(false, "GitHub repository must be configured", null);

        // Resolve token: prefer OAuth token from connected user, fall back to legacy PAT
        string? token = project.ExternalApiToken;
        if (project.GitHubConnectedByUserId.HasValue)
        {
            token = await dbContext.Users
                .Where(u => u.Id == project.GitHubConnectedByUserId.Value)
                .Select(u => u.GitHubAccessToken)
                .FirstOrDefaultAsync(cancellationToken);
        }
        if (string.IsNullOrWhiteSpace(token))
            return new TestGitHubConnectionResult(false, "No GitHub access token available. Connect your GitHub account or configure an API token.", null);

        var parts = project.ExternalProjectId.Split('/');
        if (parts.Length != 2)
            return new TestGitHubConnectionResult(false, "Invalid repository format. Use 'owner/repo'", null);

        try
        {
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("SoftimProject"))
            {
                Credentials = new Octokit.Credentials(token)
            };

            var repository = await client.Repository.Get(parts[0], parts[1]);
            return new TestGitHubConnectionResult(true, null, repository.FullName);
        }
        catch (Octokit.NotFoundException)
        {
            return new TestGitHubConnectionResult(false, "Repository not found or access denied", null);
        }
        catch (Octokit.AuthorizationException)
        {
            return new TestGitHubConnectionResult(false, "Invalid or expired API token", null);
        }
        catch (Exception ex)
        {
            return new TestGitHubConnectionResult(false, ex.Message, null);
        }
    }
}
