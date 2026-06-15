using MediatR;
using Microsoft.EntityFrameworkCore;
using Octokit;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.GitHub;

public sealed record LinkGitHubRepoCommand(Guid ProjectId, string RepositoryFullName) : IRequest, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.ProjectManager;
}

public sealed class LinkGitHubRepoCommandHandler(
    IApplicationDbContext dbContext,
    IGitHubProvisioningService provisioning,
    ICurrentUserService currentUser) : IRequestHandler<LinkGitHubRepoCommand>
{
    public async Task Handle(LinkGitHubRepoCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException();

        var token = await dbContext.Users
            .Where(u => u.Id == currentUser.UserId.Value)
            .Select(u => u.GitHubAccessToken)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("GitHub account not connected. Please connect your GitHub account first.");

        var parts = request.RepositoryFullName.Split('/');
        if (parts.Length != 2)
            throw new InvalidOperationException("Invalid repository format. Use 'owner/repo'.");

        // Verify repo exists and user has access
        var client = new GitHubClient(new ProductHeaderValue("SoftimProject"))
        {
            Credentials = new Credentials(token)
        };

        try
        {
            await client.Repository.Get(parts[0], parts[1]);
        }
        catch (Octokit.NotFoundException)
        {
            throw new InvalidOperationException("Repository not found or you don't have access.");
        }

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new Common.NotFoundException(nameof(Domain.Entities.Project), request.ProjectId);

        project.ExternalSystem = "GitHub";
        project.ExternalProjectId = request.RepositoryFullName;
        project.GitHubConnectedByUserId = currentUser.UserId.Value;

        // Auto-register the webhook + resolve GitHub App installation (best-effort).
        var provision = await provisioning.ProvisionRepoAsync(parts[0], parts[1], token, cancellationToken);
        if (provision.WebhookId.HasValue)
            project.GitHubWebhookId = provision.WebhookId;
        if (!string.IsNullOrEmpty(provision.WebhookSecret))
            project.WebhookSecret = provision.WebhookSecret;
        if (provision.InstallationId.HasValue)
            project.GitHubInstallationId = provision.InstallationId;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
