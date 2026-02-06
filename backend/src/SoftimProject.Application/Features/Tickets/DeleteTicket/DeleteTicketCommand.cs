using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Tickets.DeleteTicket;

public sealed record DeleteTicketCommand(Guid ProjectId, Guid TicketId) : IRequest, IRequireProjectAccess;

public sealed class DeleteTicketCommandHandler(
    IApplicationDbContext dbContext) : IRequestHandler<DeleteTicketCommand>
{
    public async Task Handle(DeleteTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.ProjectId == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Ticket), request.TicketId);

        dbContext.Tickets.Remove(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
