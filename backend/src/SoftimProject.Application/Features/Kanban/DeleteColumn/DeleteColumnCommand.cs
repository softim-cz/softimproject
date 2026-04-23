using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Kanban.DeleteColumn;

public sealed record DeleteColumnCommand(
    Guid ProjectId,
    Guid BoardId,
    Guid ColumnId) : IRequest, IRequireProjectRole
{
    public ProjectRole RequiredProjectRole => ProjectRole.ProjectManager;
}

public sealed class DeleteColumnCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<DeleteColumnCommand>
{
    public async Task Handle(DeleteColumnCommand request, CancellationToken cancellationToken)
    {
        var column = await dbContext.KanbanColumns
            .FirstOrDefaultAsync(c => c.Id == request.ColumnId && c.BoardId == request.BoardId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.KanbanColumn), request.ColumnId);

        dbContext.KanbanColumns.Remove(column);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
