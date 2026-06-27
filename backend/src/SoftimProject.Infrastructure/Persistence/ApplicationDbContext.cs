using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<KanbanBoard> KanbanBoards => Set<KanbanBoard>();
    public DbSet<KanbanColumn> KanbanColumns => Set<KanbanColumn>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Worklog> Worklogs => Set<Worklog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AiReport> AiReports => Set<AiReport>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<ApplicationRole> ApplicationRoles => Set<ApplicationRole>();
    public DbSet<UserApplicationRole> UserApplicationRoles => Set<UserApplicationRole>();
    public DbSet<ProjectType> ProjectTypes => Set<ProjectType>();
    public DbSet<ProjectState> ProjectStates => Set<ProjectState>();
    public DbSet<TaskType> TaskTypes => Set<TaskType>();
    public DbSet<TaskState> TaskStates => Set<TaskState>();
    public DbSet<ViewConfiguration> ViewConfigurations => Set<ViewConfiguration>();
    public DbSet<SavedFilter> SavedFilters => Set<SavedFilter>();
    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();
    public DbSet<ProjectCustomFieldValue> ProjectCustomFieldValues => Set<ProjectCustomFieldValue>();
    public DbSet<ProjectTemplate> ProjectTemplates => Set<ProjectTemplate>();
    public DbSet<ProjectTemplateField> ProjectTemplateFields => Set<ProjectTemplateField>();
    public DbSet<MigrationJob> MigrationJobs => Set<MigrationJob>();
    public DbSet<TicketCustomFieldValue> TicketCustomFieldValues => Set<TicketCustomFieldValue>();
    public DbSet<TicketPriority> TicketPriorities => Set<TicketPriority>();
    public DbSet<JobRun> JobRuns => Set<JobRun>();
    public DbSet<DeadLetterEntry> DeadLetterEntries => Set<DeadLetterEntry>();
    public DbSet<LinkedPullRequest> LinkedPullRequests => Set<LinkedPullRequest>();
    public DbSet<LinkedCommit> LinkedCommits => Set<LinkedCommit>();
    public DbSet<AiInvocation> AiInvocations => Set<AiInvocation>();
    public DbSet<ProcessedWebhookDelivery> ProcessedWebhookDeliveries => Set<ProcessedWebhookDelivery>();
    public DbSet<TicketWatcher> TicketWatchers => Set<TicketWatcher>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
