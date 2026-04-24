using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.Services;

public sealed class JobRunRecorder(IServiceScopeFactory scopeFactory, ILoggerFactory loggerFactory)
    : IJobRunRecorder
{
    public async Task<IJobRunScope> BeginAsync(string jobName, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;

        // Persist the Running row immediately so in-flight jobs show up in /health/jobs.
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            db.JobRuns.Add(new JobRun
            {
                Id = id,
                JobName = jobName,
                StartedAt = startedAt,
                Status = JobRunStatus.Running,
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        var logger = loggerFactory.CreateLogger($"JobRun.{jobName}");
        var scopeLogger = new JobRunScope(
            scopeFactory,
            logger,
            jobName,
            id,
            startedAt);
        scopeLogger.EmitStart();
        return scopeLogger;
    }

    private sealed class JobRunScope : IJobRunScope
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;
        private readonly DateTime _startedAt;
        private readonly Stopwatch _sw;
        private readonly IDisposable? _jobNameProperty;
        private readonly IDisposable? _jobRunIdProperty;
        private JobRunStatus _outcome = JobRunStatus.Running;
        private int? _processed;
        private int? _failed;
        private string? _error;
        private bool _disposed;

        public JobRunScope(
            IServiceScopeFactory scopeFactory,
            ILogger logger,
            string jobName,
            Guid jobRunId,
            DateTime startedAt)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            JobName = jobName;
            JobRunId = jobRunId;
            _startedAt = startedAt;
            _sw = Stopwatch.StartNew();
            // These disposables keep {JobName}+{JobRunId} attached to every log line
            // emitted inside the scope via Serilog LogContext.
            _jobNameProperty = LogContext.PushProperty("JobName", jobName);
            _jobRunIdProperty = LogContext.PushProperty("JobRunId", jobRunId);
        }

        public Guid JobRunId { get; }
        public string JobName { get; }

        public void EmitStart()
        {
            _logger.LogInformation(
                "Job {JobName} run {JobRunId} started at {StartedAt:o}",
                JobName, JobRunId, _startedAt);
        }

        public void MarkSuccess(int? itemsProcessed = null, int? itemsFailed = null)
        {
            _outcome = (itemsFailed ?? 0) > 0 ? JobRunStatus.PartialSuccess : JobRunStatus.Success;
            _processed = itemsProcessed;
            _failed = itemsFailed;
        }

        public void MarkPartialSuccess(int itemsProcessed, int itemsFailed, string? errorSummary = null)
        {
            _outcome = JobRunStatus.PartialSuccess;
            _processed = itemsProcessed;
            _failed = itemsFailed;
            _error = errorSummary;
        }

        public void MarkFailure(Exception exception)
        {
            _outcome = JobRunStatus.Failed;
            _error = exception.Message;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            _sw.Stop();

            var finalStatus = _outcome;
            var finalError = _error;
            if (finalStatus == JobRunStatus.Running)
            {
                // Scope disposed without a verdict — treat as failure so it's visible.
                finalStatus = JobRunStatus.Failed;
                finalError ??= "Scope disposed without an explicit outcome.";
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var row = await db.JobRuns.FindAsync(JobRunId);
                if (row is not null)
                {
                    row.CompletedAt = DateTime.UtcNow;
                    row.DurationMs = _sw.ElapsedMilliseconds;
                    row.Status = finalStatus;
                    row.ItemsProcessed = _processed;
                    row.ItemsFailed = _failed;
                    row.ErrorMessage = finalError;
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Persistence failure mustn't take down the host — log and move on.
                _logger.LogError(ex,
                    "Failed to persist JobRun completion for {JobName} {JobRunId}",
                    JobName, JobRunId);
            }

            switch (finalStatus)
            {
                case JobRunStatus.Success:
                    _logger.LogInformation(
                        "Job {JobName} run {JobRunId} finished Success in {DurationMs}ms (processed={ItemsProcessed}, failed={ItemsFailed})",
                        JobName, JobRunId, _sw.ElapsedMilliseconds, _processed, _failed);
                    break;
                case JobRunStatus.PartialSuccess:
                    _logger.LogWarning(
                        "Job {JobName} run {JobRunId} finished PartialSuccess in {DurationMs}ms (processed={ItemsProcessed}, failed={ItemsFailed}, error={Error})",
                        JobName, JobRunId, _sw.ElapsedMilliseconds, _processed, _failed, finalError);
                    break;
                case JobRunStatus.Failed:
                    _logger.LogError(
                        "Job {JobName} run {JobRunId} finished Failed in {DurationMs}ms: {Error}",
                        JobName, JobRunId, _sw.ElapsedMilliseconds, finalError);
                    break;
            }

            _jobRunIdProperty?.Dispose();
            _jobNameProperty?.Dispose();
        }
    }
}
