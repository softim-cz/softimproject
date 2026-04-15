using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Kanban.UpdateColumn;

public sealed record UpdateColumnCommand(
    Guid ProjectId,
    Guid BoardId,
    Guid ColumnId,
    string Name,
    int? WipLimit,
    List<Guid> MapsToTaskStateIds,
    string? Color) : IRequest, IRequireProjectAccess;

public sealed class UpdateColumnCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<UpdateColumnCommand>
{
    public async Task Handle(UpdateColumnCommand request, CancellationToken cancellationToken)
    {
        var column = await dbContext.KanbanColumns
            .Include(c => c.MapsToTaskStates)
            .FirstOrDefaultAsync(c => c.Id == request.ColumnId && c.BoardId == request.BoardId, cancellationToken)
            ?? throw new NotFoundException(nameof(KanbanColumn), request.ColumnId);

        var taskStates = await dbContext.TaskStates
            .Where(ts => request.MapsToTaskStateIds.Contains(ts.Id))
            .ToListAsync(cancellationToken);

        column.Name = request.Name;
        column.WipLimit = request.WipLimit;
        column.Color = request.Color;
        column.MapsToTaskStates.Clear();
        foreach (var ts in taskStates)
            column.MapsToTaskStates.Add(ts);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
