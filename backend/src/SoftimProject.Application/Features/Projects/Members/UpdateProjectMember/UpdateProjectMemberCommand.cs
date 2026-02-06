using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.Members.UpdateProjectMember;

public sealed record UpdateProjectMemberCommand(
    Guid ProjectId,
    Guid MemberId,
    ProjectRole Role,
    decimal? HourlyRateOverride) : IRequest, IRequireProjectAccess;

public sealed class UpdateProjectMemberCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<UpdateProjectMemberCommand>
{
    public async Task Handle(UpdateProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await dbContext.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.Id == request.MemberId && pm.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.ProjectMember), request.MemberId);

        member.Role = request.Role;
        member.HourlyRateOverride = request.HourlyRateOverride;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
