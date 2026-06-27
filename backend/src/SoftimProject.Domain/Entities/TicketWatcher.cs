namespace SoftimProject.Domain.Entities;

// Per-user "watching" flag on a ticket. A row exists only while the user watches
// the ticket; the absence of a row means "not watching" (the default).
public class TicketWatcher
{
    public Guid TicketId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Ticket Ticket { get; set; } = null!;
    public User User { get; set; } = null!;
}
