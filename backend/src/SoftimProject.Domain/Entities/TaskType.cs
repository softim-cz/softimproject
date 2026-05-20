using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

public class TaskType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameCs { get; set; }
    public string? NameEn { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
