using System.Linq.Expressions;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects;

public sealed record ProjectMemberDto(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string Email,
    string? AvatarUrl,
    ProjectRole Role,
    decimal? HourlyRateOverride,
    DateTime JoinedAt);

public sealed record ProjectBoardDto(
    Guid Id,
    string Name,
    bool IsDefault);

public sealed record ProjectDetailDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    ProjectStatus Status,
    Guid? CompanyId,
    string? CompanyName,
    Guid? ProjectTypeId,
    string? ProjectTypeName,
    Guid? ProjectStateId,
    string? ProjectStateName,
    string? ProjectStateColor,
    Guid? ParentProjectId,
    string? ParentProjectName,
    decimal? BudgetHours,
    decimal SpentHours,
    decimal? BudgetAmount,
    decimal SpentAmount,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateOnly? DeadlineDate,
    int HealthScore,
    bool IsOverBudget,
    bool IsOverDeadline,
    bool ClientAccessEnabled,
    string? ClientAccessToken,
    string? ExternalSystem,
    string? ExternalProjectId,
    Guid? GitHubConnectedByUserId,
    bool GitHubWebhookActive,
    int MemberCount,
    int TicketCount,
    List<ProjectMemberDto> Members,
    List<ProjectBoardDto> Boards);

internal static class ProjectDetailProjections
{
    public static readonly Expression<Func<Project, ProjectDetailDto>> Detail = project => new ProjectDetailDto(
        project.Id,
        project.Name,
        project.Code,
        project.Description,
        project.Status,
        project.CompanyId,
        project.Company != null ? project.Company.Name : null,
        project.ProjectTypeId,
        project.ProjectType != null ? project.ProjectType.Name : null,
        project.ProjectStateId,
        project.ProjectState != null ? project.ProjectState.Name : null,
        project.ProjectState != null ? project.ProjectState.Color : null,
        project.ParentProjectId,
        project.ParentProject != null ? project.ParentProject.Name : null,
        project.BudgetHours,
        project.SpentHours,
        project.BudgetAmount,
        project.SpentAmount,
        project.StartDate,
        project.EndDate,
        project.DeadlineDate,
        project.HealthScore,
        project.IsOverBudget,
        project.IsOverDeadline,
        project.ClientAccessEnabled,
        project.ClientAccessToken,
        project.ExternalSystem,
        project.ExternalProjectId,
        project.GitHubConnectedByUserId,
        project.GitHubWebhookId != null,
        project.Members.Count,
        project.Tickets.Count,
        project.Members
            .OrderBy(member => member.JoinedAt)
            .Select(member => new ProjectMemberDto(
                member.Id,
                member.UserId,
                member.User.DisplayName,
                member.User.Email,
                member.User.AvatarUrl,
                member.Role,
                member.HourlyRateOverride,
                member.JoinedAt))
            .ToList(),
        project.Boards
            .OrderByDescending(board => board.IsDefault)
            .ThenBy(board => board.Name)
            .Select(board => new ProjectBoardDto(
                board.Id,
                board.Name,
                board.IsDefault))
            .ToList());
}
