using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;

namespace SoftimProject.Application.Features.Notifications.MarkAllNotificationsRead;

public sealed record MarkAllNotificationsReadCommand : IRequest;

public sealed class MarkAllNotificationsReadCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<MarkAllNotificationsReadCommand>
{
    public async Task Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue) return;

        var unread = await dbContext.Notifications
            .Where(n => n.UserId == currentUserService.UserId.Value && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.IsRead = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
