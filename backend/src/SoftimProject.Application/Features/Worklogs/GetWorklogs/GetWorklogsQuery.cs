using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Common;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Worklogs.GetWorklogs;

public sealed record WorklogUserDto(
    Guid Id,
    string DisplayName);

public sealed record WorklogDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    Guid TicketId,
    string TicketTitle,
    Guid UserId,
    WorklogUserDto User,
    DateOnly Date,
    decimal Hours,
    string Description,
    WorklogSource Source,
    bool IsBillable,
    decimal? HourlyRateSnapshot,
    string? AiSummary,
    string? Invoiced,
    DateTime CreatedAt);

public sealed record GetWorklogsQuery(
    Guid? ProjectId = null,
    Guid? TicketId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? UserId = null,
    bool IncludeSubprojects = false,
    string? SortField = null,
    string? SortDirection = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<WorklogDto>>;

public sealed class GetWorklogsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<GetWorklogsQuery, PagedResult<WorklogDto>>
{
    public async Task<PagedResult<WorklogDto>> Handle(GetWorklogsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Worklogs.AsNoTracking().AsQueryable();

        if (request.ProjectId.HasValue)
        {
            if (request.IncludeSubprojects)
            {
                var projectIds = await GetProjectAndDescendantsAsync(request.ProjectId.Value, cancellationToken);
                query = query.Where(w => projectIds.Contains(w.Ticket.ProjectId));
            }
            else
            {
                query = query.Where(w => w.Ticket.ProjectId == request.ProjectId.Value);
            }
        }

        if (request.TicketId.HasValue)
            query = query.Where(w => w.TicketId == request.TicketId.Value);

        if (request.From.HasValue)
            query = query.Where(w => w.Date >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(w => w.Date <= request.To.Value);

        if (request.UserId.HasValue)
            query = query.Where(w => w.UserId == request.UserId.Value);

        // Without a ProjectId filter, non-admin callers only ever see their own worklogs.
        // When a ProjectId is provided, row-level visibility is enforced by project membership
        // (the controller-level IRequireProjectAccess guard, applied per request).
        if (!currentUserService.IsInRole("Admin")
            && !request.ProjectId.HasValue
            && currentUserService.UserId.HasValue)
        {
            query = query.Where(w => w.UserId == currentUserService.UserId.Value);
        }

        var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var ordered = ApplyWorklogSort(query, request.SortField, descending)
            .ThenByDescending(w => w.CreatedAt);

        var totalCount = await ordered.CountAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        // Upper bound raised to 1000 so the client can load enough worklogs in one page
        // for client-side grouping; normal paged browsing still uses 50.
        var pageSize = Math.Clamp(request.PageSize, 1, 1000);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WorklogDto(
                w.Id,
                w.Ticket.ProjectId,
                w.Ticket.Project.Name,
                w.TicketId,
                w.Ticket.Title,
                w.UserId,
                new WorklogUserDto(w.UserId, w.User.DisplayName),
                w.Date,
                w.Hours,
                w.Description,
                w.Source,
                w.IsBillable,
                w.HourlyRateSnapshot,
                w.AiSummary,
                w.Invoiced,
                w.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<WorklogDto>(items, totalCount, page, pageSize);
    }

    // Server-side worklog ordering. CreatedAt is always appended as a tiebreaker by the caller.
    private static IOrderedQueryable<Worklog> ApplyWorklogSort(
        IQueryable<Worklog> query, string? sortField, bool descending) => sortField switch
        {
            "hours" => descending ? query.OrderByDescending(w => w.Hours) : query.OrderBy(w => w.Hours),
            "user" => descending ? query.OrderByDescending(w => w.User.DisplayName) : query.OrderBy(w => w.User.DisplayName),
            "ticketTitle" => descending ? query.OrderByDescending(w => w.Ticket.Title) : query.OrderBy(w => w.Ticket.Title),
            "ticket" or "ticketKey" => descending
                ? query.OrderByDescending(w => w.Ticket.Number)
                : query.OrderBy(w => w.Ticket.Number),
            "isBillable" => descending ? query.OrderByDescending(w => w.IsBillable) : query.OrderBy(w => w.IsBillable),
            "source" => descending ? query.OrderByDescending(w => w.Source) : query.OrderBy(w => w.Source),
            "invoiced" => descending ? query.OrderByDescending(w => w.Invoiced) : query.OrderBy(w => w.Invoiced),
            "createdAt" => descending ? query.OrderByDescending(w => w.CreatedAt) : query.OrderBy(w => w.CreatedAt),
            "date" => descending ? query.OrderByDescending(w => w.Date) : query.OrderBy(w => w.Date),
            // Default preserves the previous behaviour: newest first.
            _ => query.OrderByDescending(w => w.Date),
        };

    // Returns the project plus all of its (recursive) sub-projects. Loads the project edges once
    // and walks the tree in memory; the result set is cycle-safe via the visited set.
    private async Task<HashSet<Guid>> GetProjectAndDescendantsAsync(Guid rootId, CancellationToken cancellationToken)
    {
        var edges = await dbContext.Projects
            .AsNoTracking()
            .Select(p => new { p.Id, p.ParentProjectId })
            .ToListAsync(cancellationToken);

        var childrenByParent = edges
            .Where(e => e.ParentProjectId.HasValue)
            .GroupBy(e => e.ParentProjectId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Id).ToList());

        var result = new HashSet<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(rootId);

        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!result.Add(id))
                continue;

            if (childrenByParent.TryGetValue(id, out var children))
            {
                foreach (var childId in children)
                    stack.Push(childId);
            }
        }

        return result;
    }
}
