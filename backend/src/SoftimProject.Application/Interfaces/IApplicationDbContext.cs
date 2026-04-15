using Microsoft.EntityFrameworkCore;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<KanbanBoard> KanbanBoards { get; }
    DbSet<KanbanColumn> KanbanColumns { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketAttachment> TicketAttachments { get; }
    DbSet<ChecklistItem> ChecklistItems { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Worklog> Worklogs { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AiReport> AiReports { get; }
    DbSet<SyncLog> SyncLogs { get; }
    DbSet<Company> Companies { get; }
    DbSet<ApplicationRole> ApplicationRoles { get; }
    DbSet<UserApplicationRole> UserApplicationRoles { get; }
    DbSet<ProjectType> ProjectTypes { get; }
    DbSet<ProjectState> ProjectStates { get; }
    DbSet<TaskType> TaskTypes { get; }
    DbSet<TaskState> TaskStates { get; }
    DbSet<ViewConfiguration> ViewConfigurations { get; }
    DbSet<SavedFilter> SavedFilters { get; }
    DbSet<CustomFieldDefinition> CustomFieldDefinitions { get; }
    DbSet<ProjectCustomFieldValue> ProjectCustomFieldValues { get; }
    DbSet<ProjectTemplate> ProjectTemplates { get; }
    DbSet<ProjectTemplateField> ProjectTemplateFields { get; }
    DbSet<MigrationJob> MigrationJobs { get; }
    DbSet<TicketCustomFieldValue> TicketCustomFieldValues { get; }
    DbSet<TicketPriority> TicketPriorities { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
