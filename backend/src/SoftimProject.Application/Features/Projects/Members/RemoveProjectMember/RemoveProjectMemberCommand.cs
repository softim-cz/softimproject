using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.Members.RemoveProjectMember;

public sealed record RemoveProjectMemberCommand(
    Guid ProjectId,
    Guid MemberId) : IRequest, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.ProjectManager;
}

public sealed class RemoveProjectMemberCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<RemoveProjectMemberCommand>
{
    public async Task Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await dbContext.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.Id == request.MemberId && pm.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ProjectMember), request.MemberId);

        dbContext.ProjectMembers.Remove(member);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
