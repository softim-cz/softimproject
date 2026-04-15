using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Projects;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Projects.GetProjectById;

public sealed record GetProjectByIdQuery(Guid Id) : IRequest<ProjectDetailDto>, IRequireProjectAccess
{
    public Guid ProjectId => Id;
}

public sealed class GetProjectByIdQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetProjectByIdQuery, ProjectDetailDto>
{
    public async Task<ProjectDetailDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Id == request.Id)
            .Select(ProjectDetailProjections.Detail)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.Id);
    }
}
