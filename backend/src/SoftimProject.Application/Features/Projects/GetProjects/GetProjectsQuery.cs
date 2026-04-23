using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.GetProjects;

public sealed record ProjectDto(
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
    int TicketCount);

public sealed record GetProjectsQuery(int Page = 1, int PageSize = 50) : IRequest<PagedResult<ProjectDto>>;

public sealed class GetProjectsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<GetProjectsQuery, PagedResult<ProjectDto>>
{
    public async Task<PagedResult<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Projects.AsQueryable();

        // Non-admin users only see projects they are members of
        if (!currentUserService.IsInRole("Admin"))
        {
            if (!currentUserService.UserId.HasValue)
                return new PagedResult<ProjectDto>([], 0, request.Page, request.PageSize);

            var userId = currentUserService.UserId.Value;
            query = query.Where(p => p.Members.Any(m => m.UserId == userId));
        }

        var ordered = query.OrderBy(p => p.Name);

        var totalCount = await ordered.CountAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProjectDto(
                p.Id,
                p.Name,
                p.Code,
                p.Description,
                p.Status,
                p.CompanyId,
                p.Company != null ? p.Company.Name : null,
                p.ProjectTypeId,
                p.ProjectType != null ? p.ProjectType.Name : null,
                p.ProjectStateId,
                p.ProjectState != null ? p.ProjectState.Name : null,
                p.ProjectState != null ? p.ProjectState.Color : null,
                p.ParentProjectId,
                p.ParentProject != null ? p.ParentProject.Name : null,
                p.BudgetHours,
                p.SpentHours,
                p.BudgetAmount,
                p.SpentAmount,
                p.StartDate,
                p.EndDate,
                p.DeadlineDate,
                p.HealthScore,
                p.IsOverBudget,
                p.IsOverDeadline,
                p.Members.Count,
                p.Tickets.Count))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectDto>(items, totalCount, page, pageSize);
    }
}
