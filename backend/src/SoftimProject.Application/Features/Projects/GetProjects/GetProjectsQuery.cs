using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Projects.GetProjects;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    ProjectStatus Status,
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

public sealed record GetProjectsQuery : IRequest<List<ProjectDto>>;

public sealed class GetProjectsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<GetProjectsQuery, List<ProjectDto>>
{
    public async Task<List<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Projects.AsQueryable();

        // Non-admin users only see projects they are members of
        if (!currentUserService.IsInRole("Admin") && currentUserService.UserId.HasValue)
        {
            var userId = currentUserService.UserId.Value;
            query = query.Where(p => p.Members.Any(m => m.UserId == userId));
        }

        return await query
            .Select(p => new ProjectDto(
                p.Id,
                p.Name,
                p.Code,
                p.Description,
                p.Status,
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
    }
}
