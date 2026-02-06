using SoftimProject.Domain.Common;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

public class Comment : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty; // Markdown
    public bool IsInternal { get; set; }
    public CommentSource Source { get; set; }
    public string? ExternalId { get; set; }

    // Navigation properties
    public Ticket Ticket { get; set; } = null!;
    public User Author { get; set; } = null!;
}
