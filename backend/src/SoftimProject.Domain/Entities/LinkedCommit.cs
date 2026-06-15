namespace SoftimProject.Domain.Entities;

// One row per external commit linked to a ticket. Discovered at push-webhook time
// by parsing the commit message for a ticket key (e.g. PROJ-42). Upserted by
// (Provider, Sha, TicketId) so webhook replays are idempotent.
public class LinkedCommit
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public string Provider { get; set; } = "GitHub";
    public string Sha { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? AuthorLogin { get; set; }
    public DateTime CommittedAt { get; set; }
}
