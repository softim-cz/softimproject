using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Tickets;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Tickets.GetTicketById;

public sealed record GetTicketByIdQuery(Guid ProjectId, Guid TicketId) : IRequest<TicketDetailDto>, IRequireProjectAccess;

public sealed class GetTicketByIdQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetTicketByIdQuery, TicketDetailDto>
{
    public async Task<TicketDetailDto> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == request.TicketId && ticket.ProjectId == request.ProjectId)
            .Select(TicketDetailProjections.Detail)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Ticket), request.TicketId);
    }
}
