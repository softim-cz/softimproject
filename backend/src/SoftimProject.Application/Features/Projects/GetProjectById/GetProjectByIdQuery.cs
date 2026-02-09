using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Projects.GetProjects;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.GetProjectById;

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
    int MemberCount,
    int TicketCount,
    List<ProjectMemberDto> Members,
    List<ProjectBoardDto> Boards);

public sealed record GetProjectByIdQuery(Guid Id) : IRequest<ProjectDetailDto>, IRequireProjectAccess
{
    public Guid ProjectId => Id;
}

public sealed class GetProjectByIdQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetProjectByIdQuery, ProjectDetailDto>
{
    public async Task<ProjectDetailDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .Include(p => p.Members).ThenInclude(m => m.User)
            .Include(p => p.Boards)
            .Include(p => p.Tickets)
            .Include(p => p.Company)
            .Include(p => p.ProjectType)
            .Include(p => p.ProjectState)
            .Include(p => p.ParentProject)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project), request.Id);

        var members = project.Members.Select(m => new ProjectMemberDto(
            m.Id,
            m.UserId,
            m.User.DisplayName,
            m.User.Email,
            m.User.AvatarUrl,
            m.Role,
            m.HourlyRateOverride,
            m.JoinedAt)).ToList();

        var boards = project.Boards.Select(b => new ProjectBoardDto(
            b.Id,
            b.Name,
            b.IsDefault)).ToList();

        return new ProjectDetailDto(
            project.Id,
            project.Name,
            project.Code,
            project.Description,
            project.Status,
            project.CompanyId,
            project.Company?.Name,
            project.ProjectTypeId,
            project.ProjectType?.Name,
            project.ProjectStateId,
            project.ProjectState?.Name,
            project.ProjectState?.Color,
            project.ParentProjectId,
            project.ParentProject?.Name,
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
            project.Members.Count,
            project.Tickets.Count,
            members,
            boards);
    }
}
