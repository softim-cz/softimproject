using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

public class ProjectType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
