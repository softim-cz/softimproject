using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

public class SyncLog
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public SyncType SyncType { get; set; }
    public SyncStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ItemsSynced { get; set; }
    public int ItemsFailed { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
}
