using SoftimProject.Domain.Common;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // 2-6 chars uppercase
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; }

    // Lookup FK
    public Guid? CompanyId { get; set; }
    public Guid? ProjectTypeId { get; set; }
    public Guid? ProjectStateId { get; set; }
    public Guid? ParentProjectId { get; set; }

    // Budget fields
    public decimal? BudgetHours { get; set; }
    public decimal SpentHours { get; set; }
    public decimal? BudgetAmount { get; set; }
    public decimal SpentAmount { get; set; }

    // Timeline
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? DeadlineDate { get; set; }

    // Health
    public int HealthScore { get; set; } // 0-100
    public bool IsOverBudget { get; set; }
    public bool IsOverDeadline { get; set; }

    // External sync
    public string? ExternalSystem { get; set; } // e.g. "Jira"
    public string? ExternalProjectId { get; set; }
    public string? ExternalBaseUrl { get; set; }
    public string? ExternalApiToken { get; set; }

    // Client access
    public string? ClientAccessToken { get; set; }
    public bool ClientAccessEnabled { get; set; }

    // Navigation properties
    public Company? Company { get; set; }
    public ProjectType? ProjectType { get; set; }
    public ProjectState? ProjectState { get; set; }
    public Project? ParentProject { get; set; }
    public ICollection<Project> SubProjects { get; set; } = new List<Project>();
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public ICollection<KanbanBoard> Boards { get; set; } = new List<KanbanBoard>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();
    public ICollection<AiReport> AiReports { get; set; } = new List<AiReport>();
    public ICollection<SyncLog> SyncLogs { get; set; } = new List<SyncLog>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<ViewConfiguration> ViewConfigurations { get; set; } = new List<ViewConfiguration>();
}
