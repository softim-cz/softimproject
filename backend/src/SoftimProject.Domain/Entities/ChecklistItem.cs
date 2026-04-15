using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

public class ChecklistItem : BaseEntity
{
    public Guid TicketId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int Position { get; set; }
    public string? ExternalId { get; set; }

    // Navigation properties
    public Ticket Ticket { get; set; } = null!;
}
