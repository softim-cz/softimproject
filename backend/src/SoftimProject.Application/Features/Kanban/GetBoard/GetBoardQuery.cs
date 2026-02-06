using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Kanban.GetBoard;

public sealed record BoardTicketDto(
    Guid Id,
    string Title,
    TicketPriority Priority,
    TicketStatus Status,
    double Position,
    Guid? AssigneeId,
    string? AssigneeDisplayName,
    DateOnly? DueDate,
    decimal? EstimatedHours);

public sealed record BoardColumnDto(
    Guid Id,
    string Name,
    int Position,
    int? WipLimit,
    TicketStatus MapsToStatus,
    List<BoardTicketDto> Tickets);

public sealed record BoardDto(
    Guid Id,
    string Name,
    bool IsDefault,
    Guid ProjectId,
    List<BoardColumnDto> Columns);

public sealed record GetBoardQuery(Guid BoardId, Guid ProjectId) : IRequest<BoardDto>, IRequireProjectAccess;

public sealed class GetBoardQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetBoardQuery, BoardDto>
{
    public async Task<BoardDto> Handle(GetBoardQuery request, CancellationToken cancellationToken)
    {
        var board = await dbContext.KanbanBoards
            .Include(b => b.Columns.OrderBy(c => c.Position))
                .ThenInclude(c => c.Tickets.OrderBy(t => t.Position))
                    .ThenInclude(t => t.Assignee)
            .FirstOrDefaultAsync(b => b.Id == request.BoardId && b.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.KanbanBoard), request.BoardId);

        var columns = board.Columns.Select(c => new BoardColumnDto(
            c.Id,
            c.Name,
            c.Position,
            c.WipLimit,
            c.MapsToStatus,
            c.Tickets.Select(t => new BoardTicketDto(
                t.Id,
                t.Title,
                t.Priority,
                t.Status,
                t.Position,
                t.AssigneeId,
                t.Assignee?.DisplayName,
                t.DueDate,
                t.EstimatedHours)).ToList()
        )).ToList();

        return new BoardDto(
            board.Id,
            board.Name,
            board.IsDefault,
            board.ProjectId,
            columns);
    }
}
