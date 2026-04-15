namespace SoftimProject.Application.Features.Migration.EasyProject.Dtos;

public sealed record MigrationJobDto(
    Guid Id,
    string SourceSystem,
    string SourceBaseUrl,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int ProjectsMigrated,
    int TicketsMigrated,
    int ItemsFailed);
