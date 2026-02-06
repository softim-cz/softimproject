using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Interfaces;

public interface INotificationService
{
    Task SendAsync(Guid userId, string title, string? message, NotificationType type, Guid? referenceId = null, string? referenceType = null, CancellationToken cancellationToken = default);
    Task SendToProjectAsync(Guid projectId, string title, string? message, NotificationType type, Guid? referenceId = null, string? referenceType = null, CancellationToken cancellationToken = default);
}
