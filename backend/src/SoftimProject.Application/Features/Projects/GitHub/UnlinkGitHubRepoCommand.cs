using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.GitHub;

public sealed record UnlinkGitHubRepoCommand(Guid ProjectId) : IRequest, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.ProjectManager;
}

public sealed class UnlinkGitHubRepoCommandHandler(
    IApplicationDbContext dbContext,
    IGitHubProvisioningService provisioning) : IRequestHandler<UnlinkGitHubRepoCommand>
{
    public async Task Handle(UnlinkGitHubRepoCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.ProjectId);

        // Best-effort: remove the auto-registered webhook using the connecting user's token.
        if (project.GitHubWebhookId.HasValue
            && project.GitHubConnectedByUserId.HasValue
            && !string.IsNullOrWhiteSpace(project.ExternalProjectId))
        {
            var token = await dbContext.Users
                .Where(u => u.Id == project.GitHubConnectedByUserId.Value)
                .Select(u => u.GitHubAccessToken)
                .FirstOrDefaultAsync(cancellationToken);
            var parts = project.ExternalProjectId.Split('/');
            if (!string.IsNullOrWhiteSpace(token) && parts.Length == 2)
                await provisioning.RemoveWebhookAsync(parts[0], parts[1], token, project.GitHubWebhookId.Value, cancellationToken);
        }

        project.ExternalSystem = null;
        project.ExternalProjectId = null;
        project.GitHubConnectedByUserId = null;
        project.GitHubWebhookId = null;
        project.WebhookSecret = null;
        project.GitHubInstallationId = null;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
