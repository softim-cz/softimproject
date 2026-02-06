using SoftimProject.Domain.Common;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

public class Ticket : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid? ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; } // Markdown
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; }
    public double Position { get; set; } // For drag-drop ordering
    public Guid? AssigneeId { get; set; }
    public Guid ReporterId { get; set; }
    public string? ExternalId { get; set; }
    public string? ExternalUrl { get; set; }
    public string? AiSummary { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal? EstimatedHours { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public KanbanColumn? Column { get; set; }
    public User? Assignee { get; set; }
    public User Reporter { get; set; } = null!;
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();
}
