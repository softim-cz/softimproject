using SoftimProject.Domain.Common;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

public class MigrationJob : BaseEntity
{
    public Guid InitiatedByUserId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string SourceBaseUrl { get; set; } = string.Empty;
    public MigrationStatus Status { get; set; }
    // Last successfully completed phase. Lets a Resume skip work that already
    // ran. Advances inside `EasyProjectMigrationService.ExecuteAsync` with an
    // explicit SaveChanges at each boundary, so a crash stays on the last
    // boundary rather than an in-flight batch.
    public MigrationPhase CurrentPhase { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Counters
    public int ProjectsTotal { get; set; }
    public int ProjectsMigrated { get; set; }
    public int TicketsTotal { get; set; }
    public int TicketsMigrated { get; set; }
    public int CommentsTotal { get; set; }
    public int CommentsMigrated { get; set; }
    public int WorklogsTotal { get; set; }
    public int WorklogsMigrated { get; set; }
    public int AttachmentsTotal { get; set; }
    public int AttachmentsMigrated { get; set; }
    public int ItemsFailed { get; set; }
    public int ItemsSkipped { get; set; }
    public int ItemsUpdated { get; set; }
    public int ItemsCreated { get; set; }

    public string? ErrorLog { get; set; }
    public string? Configuration { get; set; }

    // Navigation properties
    public User InitiatedBy { get; set; } = null!;
}
