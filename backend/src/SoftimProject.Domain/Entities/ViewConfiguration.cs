using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

public class ViewConfiguration : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? ProjectId { get; set; }
    public string ViewType { get; set; } = string.Empty; // "TaskList", "Kanban", "Worklogs"
    public string ConfigurationJson { get; set; } = "{}"; // JSON blob for column widths, visibility, etc.

    // Navigation properties
    public User User { get; set; } = null!;
    public Project? Project { get; set; }
}
