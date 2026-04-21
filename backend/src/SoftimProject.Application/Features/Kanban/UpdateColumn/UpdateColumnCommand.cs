using FluentValidation;
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
    string? Color,
    bool IsVisible = true) : IRequest, IRequireProjectAccess;

public sealed class UpdateColumnCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<UpdateColumnCommand>
{
    public async Task Handle(UpdateColumnCommand request, CancellationToken cancellationToken)
    {
        var column = await dbContext.KanbanColumns
            .Include(c => c.MapsToTaskStates)
            .Include(c => c.Tickets)
            .FirstOrDefaultAsync(c => c.Id == request.ColumnId && c.BoardId == request.BoardId, cancellationToken)
            ?? throw new NotFoundException(nameof(KanbanColumn), request.ColumnId);

        // Cannot hide a column that still has tickets — move them first.
        if (!request.IsVisible && column.Tickets.Count > 0)
        {
            throw new ValidationException(
                $"Column '{column.Name}' has {column.Tickets.Count} ticket(s). Move them to another column before hiding.");
        }

        var taskStates = await dbContext.TaskStates
            .Where(ts => request.MapsToTaskStateIds.Contains(ts.Id))
            .ToListAsync(cancellationToken);

        // Take over any state currently mapped by another column on the same board.
        var conflictingColumns = await dbContext.KanbanColumns
            .Include(c => c.MapsToTaskStates)
            .Where(c => c.BoardId == request.BoardId
                && c.Id != request.ColumnId
                && c.MapsToTaskStates.Any(ts => request.MapsToTaskStateIds.Contains(ts.Id)))
            .ToListAsync(cancellationToken);

        foreach (var otherColumn in conflictingColumns)
        {
            var toRemove = otherColumn.MapsToTaskStates
                .Where(ts => request.MapsToTaskStateIds.Contains(ts.Id))
                .ToList();
            foreach (var ts in toRemove)
                otherColumn.MapsToTaskStates.Remove(ts);
        }

        column.Name = request.Name;
        column.WipLimit = request.WipLimit;
        column.Color = request.Color;
        column.IsVisible = request.IsVisible;
        column.MapsToTaskStates.Clear();
        foreach (var ts in taskStates)
            column.MapsToTaskStates.Add(ts);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
