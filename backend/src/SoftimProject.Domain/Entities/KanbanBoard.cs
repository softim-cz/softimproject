using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

public class KanbanBoard : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public ICollection<KanbanColumn> Columns { get; set; } = new List<KanbanColumn>();
}
