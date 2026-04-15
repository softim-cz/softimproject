namespace SoftimProject.Application.Features.Migration.EasyProject.Dtos;

public sealed record EpProjectPreviewDto(
    int EpId,
    string Name,
    string? Description,
    int Status,
    string? ParentName,
    int IssueCount,
    bool AlreadyImported);
