namespace SoftimProject.Application.Features.Migration.EasyProject.Dtos;

public sealed record MigrationProgressDto(
    Guid JobId,
    string Status,
    string CurrentPhase,
    int ProjectsTotal,
    int ProjectsMigrated,
    int TicketsTotal,
    int TicketsMigrated,
    int CommentsTotal,
    int CommentsMigrated,
    int WorklogsTotal,
    int WorklogsMigrated,
    int AttachmentsTotal,
    int AttachmentsMigrated,
    int ErrorCount,
    int ItemsCreated,
    int ItemsUpdated,
    int ItemsSkipped,
    List<string> RecentErrors,
    List<string> RecentLog,
    int OverallPercent);
