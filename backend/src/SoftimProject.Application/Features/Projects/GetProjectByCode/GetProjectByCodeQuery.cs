using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Projects;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Projects.GetProjectByCode;

public sealed record GetProjectByCodeQuery(string Code) : IRequest<ProjectDetailDto>;

public sealed class GetProjectByCodeQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetProjectByCodeQuery, ProjectDetailDto>
{
    public async Task<ProjectDetailDto> Handle(GetProjectByCodeQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Code == request.Code)
            .Select(ProjectDetailProjections.Detail)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.Code);
    }
}
