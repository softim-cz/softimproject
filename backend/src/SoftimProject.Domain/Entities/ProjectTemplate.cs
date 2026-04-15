using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

public class ProjectTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<ProjectTemplateField> Fields { get; set; } = new List<ProjectTemplateField>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<TaskState> TaskStates { get; set; } = new List<TaskState>();
    public ICollection<TicketPriority> TicketPriorities { get; set; } = new List<TicketPriority>();
}
