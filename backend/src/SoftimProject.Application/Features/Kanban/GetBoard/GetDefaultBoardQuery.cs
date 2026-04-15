using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Kanban.GetBoard;

public sealed record GetDefaultBoardQuery(Guid ProjectId) : IRequest<BoardDto>, IRequireProjectAccess;

public sealed class GetDefaultBoardQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetDefaultBoardQuery, BoardDto>
{
    public async Task<BoardDto> Handle(GetDefaultBoardQuery request, CancellationToken cancellationToken)
    {
        var board = await dbContext.KanbanBoards
            .Include(b => b.Project)
            .Include(b => b.Columns.OrderBy(c => c.Position))
                .ThenInclude(c => c.MapsToTaskStates.OrderBy(ts => ts.SortOrder))
            .Include(b => b.Columns.OrderBy(c => c.Position))
                .ThenInclude(c => c.Tickets.OrderBy(t => t.Position))
                    .ThenInclude(t => t.Assignee)
            .Include(b => b.Columns.OrderBy(c => c.Position))
                .ThenInclude(c => c.Tickets.OrderBy(t => t.Position))
                    .ThenInclude(t => t.TaskType)
            .Include(b => b.Columns.OrderBy(c => c.Position))
                .ThenInclude(c => c.Tickets.OrderBy(t => t.Position))
                    .ThenInclude(t => t.TaskState)
            .Include(b => b.Columns.OrderBy(c => c.Position))
                .ThenInclude(c => c.Tickets.OrderBy(t => t.Position))
                    .ThenInclude(t => t.TicketPriority)
            .Where(b => b.ProjectId == request.ProjectId)
            .OrderByDescending(b => b.IsDefault)
            .ThenBy(b => b.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("KanbanBoard", $"default for project {request.ProjectId}");

        var columns = board.Columns.Select(c => new BoardColumnDto(
            c.Id,
            c.Name,
            c.Position,
            c.WipLimit,
            c.Color,
            c.MapsToTaskStates.Select(ts => new BoardColumnTaskStateDto(
                ts.Id,
                ts.Name,
                ts.Color)).ToList(),
            c.Tickets.Select(t => new BoardTicketDto(
                t.Id,
                t.Number,
                board.Project.Code + "-" + t.Number,
                t.Title,
                t.TicketPriorityId,
                t.TicketPriority.Name,
                t.TicketPriority.Color,
                t.Position,
                t.AssigneeId,
                t.Assignee?.DisplayName,
                t.DueDate,
                t.EstimatedHours,
                t.TaskTypeId,
                t.TaskType?.Name,
                t.TaskType?.Icon,
                t.TaskStateId,
                t.TaskState.Name,
                t.TaskState.Color)).ToList()
        )).ToList();

        return new BoardDto(
            board.Id,
            board.Name,
            board.IsDefault,
            board.ProjectId,
            columns);
    }
}
