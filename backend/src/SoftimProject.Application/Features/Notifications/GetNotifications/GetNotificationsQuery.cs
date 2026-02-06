using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Notifications.GetNotifications;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string? Message,
    NotificationType Type,
    Guid? ReferenceId,
    string? ReferenceType,
    bool IsRead,
    DateTime CreatedAt);

public sealed record GetNotificationsQuery : IRequest<List<NotificationDto>>;

public sealed class GetNotificationsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<GetNotificationsQuery, List<NotificationDto>>
{
    public async Task<List<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
            return [];

        return await dbContext.Notifications
            .Where(n => n.UserId == currentUserService.UserId.Value)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.ReferenceId,
                n.ReferenceType,
                n.IsRead,
                n.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
