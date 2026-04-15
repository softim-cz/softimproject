namespace SoftimProject.Domain.Entities;

public class KanbanColumn
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public int? WipLimit { get; set; }
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public KanbanBoard Board { get; set; } = null!;
    public ICollection<TaskState> MapsToTaskStates { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
