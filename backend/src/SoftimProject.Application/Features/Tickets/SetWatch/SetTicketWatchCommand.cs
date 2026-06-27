using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Features.Tickets.SetWatch;

public sealed record SetTicketWatchCommand(Guid ProjectId, Guid TicketId, bool Watching)
    : IRequest, IRequireProjectAccess;

public sealed class SetTicketWatchCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<SetTicketWatchCommand>
{
    public async Task Handle(SetTicketWatchCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Current user is not initialized.");

        // Ensures the ticket exists and belongs to the project (throws NotFound otherwise).
        _ = await dbContext.GetTicketForProjectAsync(request.ProjectId, request.TicketId, cancellationToken);

        var existing = await dbContext.TicketWatchers
            .FirstOrDefaultAsync(
                watcher => watcher.TicketId == request.TicketId && watcher.UserId == userId,
                cancellationToken);

        if (request.Watching && existing is null)
        {
            dbContext.TicketWatchers.Add(new TicketWatcher
            {
                TicketId = request.TicketId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (!request.Watching && existing is not null)
        {
            dbContext.TicketWatchers.Remove(existing);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
