using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

// One row per hosted-service iteration. Separate from SyncLog: SyncLog is per-project
// audit for integration sync jobs, JobRun is system-wide observability across every
// BackgroundService tick (health, deadlines, reports, AI, sync umbrellas, ...).
public class JobRun
{
    public Guid Id { get; set; }
    public string JobName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public JobRunStatus Status { get; set; }
    public long? DurationMs { get; set; }
    public int? ItemsProcessed { get; set; }
    public int? ItemsFailed { get; set; }
    public string? ErrorMessage { get; set; }
}
