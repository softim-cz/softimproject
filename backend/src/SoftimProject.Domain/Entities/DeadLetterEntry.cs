using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

// One row per operation that exhausted its retry budget. Identity in the business
// sense is (OperationType, OperationKey) — repeated failures of the same unit update
// the same row (AttemptCount++, LastError, LastFailedAt) rather than multiplying.
public class DeadLetterEntry
{
    public Guid Id { get; set; }
    public DeadLetterOperation OperationType { get; set; }
    // Stable key for the failed unit (e.g. ticket id for AI summarize, project id for
    // a sync iteration, webhook delivery id). Enables upsert-on-failure + replay lookup.
    public string OperationKey { get; set; } = string.Empty;
    // JSON blob with enough context for manual inspection / replay.
    public string Payload { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public string LastError { get; set; } = string.Empty;
    public DateTime FirstFailedAt { get; set; }
    public DateTime LastFailedAt { get; set; }
    public DeadLetterStatus Status { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedByUserId { get; set; }
}
