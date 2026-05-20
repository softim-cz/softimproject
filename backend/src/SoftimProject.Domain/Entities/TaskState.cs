using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

public class TaskState : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameCs { get; set; }
    public string? NameEn { get; set; }
    public string Color { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public bool IsClosedState { get; set; }

    // Template FK
    public Guid ProjectTemplateId { get; set; }
    public ProjectTemplate ProjectTemplate { get; set; } = null!;

    // Navigation properties
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<KanbanColumn> KanbanColumns { get; set; } = new List<KanbanColumn>();
}
