using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

public class SavedFilter : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? UserId { get; set; } // null = system filter
    public Guid? ProjectId { get; set; }
    public string ViewType { get; set; } = string.Empty; // "TaskList", "Kanban", "Worklogs"
    public string FilterJson { get; set; } = "{}";
    public bool IsSystem { get; set; }
    public int SortOrder { get; set; }

    // Navigation
    public User? User { get; set; }
    public Project? Project { get; set; }
}
