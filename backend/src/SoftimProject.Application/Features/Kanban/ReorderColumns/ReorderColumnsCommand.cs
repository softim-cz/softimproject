using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Kanban.ReorderColumns;

public sealed record ReorderColumnsCommand(
    Guid ProjectId,
    Guid BoardId,
    List<Guid> ColumnIds) : IRequest, IRequireProjectAccess;

public sealed class ReorderColumnsCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<ReorderColumnsCommand>
{
    public async Task Handle(ReorderColumnsCommand request, CancellationToken cancellationToken)
    {
        var columns = await dbContext.KanbanColumns
            .Where(c => c.BoardId == request.BoardId)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < request.ColumnIds.Count; i++)
        {
            var column = columns.FirstOrDefault(c => c.Id == request.ColumnIds[i]);
            if (column is not null)
            {
                column.Position = i;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
