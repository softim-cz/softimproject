using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Projects;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Projects.GetProjectByCode;

public sealed record GetProjectByCodeQuery(string Code) : IRequest<ProjectDetailDto>;

public sealed class GetProjectByCodeQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<GetProjectByCodeQuery, ProjectDetailDto>
{
    public async Task<ProjectDetailDto> Handle(GetProjectByCodeQuery request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Code == request.Code)
            .Select(ProjectDetailProjections.Detail)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.Code);

        // IRequireProjectAccess cannot be used here — ProjectId isn't known on the inbound request.
        // Enforce membership post-lookup; surface as NotFound to avoid leaking project codes.
        var hasAccess = await currentUserService.HasProjectAccessAsync(project.Id, cancellationToken);
        if (!hasAccess)
            throw new NotFoundException(nameof(Domain.Entities.Project), request.Code);

        return project;
    }
}
