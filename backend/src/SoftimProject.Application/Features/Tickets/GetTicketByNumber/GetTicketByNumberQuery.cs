using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Tickets;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Tickets.GetTicketByNumber;

public sealed record GetTicketByNumberQuery(Guid ProjectId, int Number) : IRequest<TicketDetailDto>, IRequireProjectAccess;

public sealed class GetTicketByNumberQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetTicketByNumberQuery, TicketDetailDto>
{
    public async Task<TicketDetailDto> Handle(GetTicketByNumberQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.ProjectId == request.ProjectId && ticket.Number == request.Number)
            .Select(TicketDetailProjections.Detail)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Ticket), $"#{request.Number} in project {request.ProjectId}");
    }
}
