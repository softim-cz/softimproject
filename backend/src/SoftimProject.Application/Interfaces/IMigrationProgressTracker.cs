using SoftimProject.Application.Features.Migration.EasyProject.Dtos;

namespace SoftimProject.Application.Interfaces;

public interface IMigrationProgressTracker
{
    void Init(Guid jobId);
    void UpdatePhase(Guid jobId, string phase);
    void UpdateCounts(Guid jobId, string entity, int total, int migrated);
    void IncrementCreated(Guid jobId);
    void IncrementUpdated(Guid jobId);
    void IncrementSkipped(Guid jobId);
    void IncrementFailed(Guid jobId);
    void AddError(Guid jobId, string error);
    void AddLog(Guid jobId, string message);
    void Complete(Guid jobId, bool hasErrors);
    void Fail(Guid jobId, string error);
    void Cancel(Guid jobId);
    MigrationProgressDto? GetProgress(Guid jobId);
    CancellationToken GetCancellationToken(Guid jobId);
    void RequestCancellation(Guid jobId);
}
