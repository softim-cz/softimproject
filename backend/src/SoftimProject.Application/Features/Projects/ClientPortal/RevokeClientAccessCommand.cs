using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Projects.ClientPortal;

public sealed record RevokeClientAccessCommand(Guid ProjectId) : IRequest, IRequireProjectAccess;

public sealed class RevokeClientAccessCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<RevokeClientAccessCommand>
{
    public async Task Handle(RevokeClientAccessCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        project.ClientAccessToken = null;
        project.ClientAccessEnabled = false;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
