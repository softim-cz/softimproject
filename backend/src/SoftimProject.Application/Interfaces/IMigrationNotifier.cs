using SoftimProject.Application.Features.Migration.EasyProject.Dtos;

namespace SoftimProject.Application.Interfaces;

public interface IMigrationNotifier
{
    Task NotifyProgressAsync(Guid jobId, MigrationProgressDto progress);
    Task SendFetchProgressAsync(string sessionId, string message, int current, int total);
    Task SendIssueCountAsync(string sessionId, int epId, int issueCount);
}
