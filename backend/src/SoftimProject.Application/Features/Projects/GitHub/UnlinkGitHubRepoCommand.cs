using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Projects.GitHub;

public sealed record UnlinkGitHubRepoCommand(Guid ProjectId) : IRequest, IRequireProjectAccess;

public sealed class UnlinkGitHubRepoCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<UnlinkGitHubRepoCommand>
{
    public async Task Handle(UnlinkGitHubRepoCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.ProjectId);

        project.ExternalSystem = null;
        project.ExternalProjectId = null;
        project.GitHubConnectedByUserId = null;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
