namespace SoftimProject.Domain.Enums;

public enum MigrationStatus
{
    Pending,
    FetchingData,
    MigratingProjects,
    MigratingTickets,
    MigratingComments,
    MigratingWorklogs,
    MigratingAttachments,
    Completed,
    CompletedWithErrors,
    Failed,
    Cancelled
}
