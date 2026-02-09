using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

public class ProjectState : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }

    // Navigation properties
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
