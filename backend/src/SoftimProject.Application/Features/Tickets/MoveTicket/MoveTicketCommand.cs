using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Tickets.MoveTicket;

public sealed record MoveTicketCommand(
    Guid ProjectId,
    Guid TicketId,
    Guid ColumnId,
    double Position) : IRequest, IRequireProjectAccess;

public sealed class MoveTicketCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<MoveTicketCommand>
{
    public async Task Handle(MoveTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Ticket), request.TicketId);

        var column = await dbContext.KanbanColumns
            .Include(c => c.MapsToTaskStates)
            .FirstOrDefaultAsync(c => c.Id == request.ColumnId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.KanbanColumn), request.ColumnId);

        ticket.ColumnId = request.ColumnId;
        ticket.Position = request.Position;

        // Keep current state if it's already mapped to this column, otherwise use the first mapped state
        if (column.MapsToTaskStates.Count > 0 && !column.MapsToTaskStates.Any(ts => ts.Id == ticket.TaskStateId))
        {
            ticket.TaskStateId = column.MapsToTaskStates.OrderBy(ts => ts.SortOrder).First().Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
