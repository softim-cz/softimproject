namespace SoftimProject.Application.Interfaces;

// Frames one background-service iteration. Begin() writes a Running row and pushes
// {JobName}+{JobRunId} onto the Serilog LogContext so every downstream log line in the
// scope gets those properties automatically. Disposal flushes the final status (Success,
// PartialSuccess, Failed) with duration. If the scope is disposed without an explicit
// outcome it's recorded as Failed("no outcome reported") — that way forgetting to mark
// a run shows up as a visible failure instead of a silent "still running" row.
public interface IJobRunRecorder
{
    Task<IJobRunScope> BeginAsync(string jobName, CancellationToken cancellationToken = default);
}

public interface IJobRunScope : IAsyncDisposable
{
    Guid JobRunId { get; }
    string JobName { get; }

    void MarkSuccess(int? itemsProcessed = null, int? itemsFailed = null);
    void MarkPartialSuccess(int itemsProcessed, int itemsFailed, string? errorSummary = null);
    void MarkFailure(Exception exception);
}
