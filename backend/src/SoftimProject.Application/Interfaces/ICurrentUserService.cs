namespace SoftimProject.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? EntraObjectId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<bool> HasProjectAccessAsync(Guid projectId, CancellationToken cancellationToken = default);
}
