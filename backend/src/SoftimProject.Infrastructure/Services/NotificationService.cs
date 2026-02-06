using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Services;

public sealed class NotificationService(IApplicationDbContext dbContext) : INotificationService
{
    public async Task SendAsync(Guid userId, string title, string? message, NotificationType type, Guid? referenceId = null, string? referenceType = null, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            ReferenceId = referenceId,
            ReferenceType = referenceType,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SendToProjectAsync(Guid projectId, string title, string? message, NotificationType type, Guid? referenceId = null, string? referenceType = null, CancellationToken cancellationToken = default)
    {
        var memberUserIds = await dbContext.ProjectMembers
            .Where(pm => pm.ProjectId == projectId)
            .Select(pm => pm.UserId)
            .ToListAsync(cancellationToken);

        var notifications = memberUserIds.Select(userId => new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            ReferenceId = referenceId,
            ReferenceType = referenceType,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Notifications.AddRange(notifications);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
