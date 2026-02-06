using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Projects.DeleteProject;

public sealed record DeleteProjectCommand(Guid Id) : IRequest, IRequireProjectAccess
{
    public Guid ProjectId => Id;
}

public sealed class DeleteProjectCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<DeleteProjectCommand>
{
    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.Id);

        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
