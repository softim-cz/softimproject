using SoftimProject.Domain.Common;

namespace SoftimProject.Domain.Entities;

public class Ticket : BaseEntity
{
    public Guid ProjectId { get; set; }
    public int Number { get; set; }
    public Guid? ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; } // Markdown
    public Guid TicketPriorityId { get; set; }
    public Guid TaskStateId { get; set; }
    public double Position { get; set; } // For drag-drop ordering
    public Guid? AssigneeId { get; set; }
    public Guid ReporterId { get; set; }
    public string? ExternalId { get; set; }
    public string? ExternalUrl { get; set; }
    public string? AiSummary { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal? EstimatedHours { get; set; }

    // Lookup FK
    public Guid? TaskTypeId { get; set; }
    public Guid? ParentTicketId { get; set; }

    // Extended fields
    public decimal CumulativeWorkedHours { get; set; }
    public decimal? ExternalBudget { get; set; }
    public string? ExternalUser { get; set; }
    public string? ExternalProject { get; set; } // Name of the external project (e.g. EasyProject source)
    public string? ImplementationNotes { get; set; }
    public string? LastComment { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public KanbanColumn? Column { get; set; }
    public User? Assignee { get; set; }
    public User Reporter { get; set; } = null!;
    public TaskType? TaskType { get; set; }
    public TaskState TaskState { get; set; } = null!;
    public TicketPriority TicketPriority { get; set; } = null!;
    public Ticket? ParentTicket { get; set; }
    public ICollection<Ticket> SubTickets { get; set; } = new List<Ticket>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();
    public ICollection<TicketCustomFieldValue> CustomFieldValues { get; set; } = new List<TicketCustomFieldValue>();
    public ICollection<TicketWatcher> Watchers { get; set; } = new List<TicketWatcher>();
}
