using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

// One row per external pull request linked to a ticket. Discovery happens at webhook
// time — branch name convention (<ProjectCode>-<TicketNumber>) or a "#<number>"
// reference in the PR body resolves to a ticket, then we upsert this record.
public class LinkedPullRequest
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    // External identity — stable across updates so we can upsert on webhook replays.
    public string Provider { get; set; } = "GitHub";
    public string ExternalId { get; set; } = string.Empty;  // PR number as string
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string? AuthorLogin { get; set; }

    public PullRequestState State { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? MergedAt { get; set; }
}
