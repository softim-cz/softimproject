using MediatR;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Application.Features.Admin;

public sealed record AiUsageProjectRow(
    Guid? ProjectId,
    string? ProjectName,
    int InvocationCount,
    long TotalTokens,
    decimal TotalCostUsd,
    int FailureCount);

public sealed record AiUsageDto(
    int DaysWindow,
    int TotalInvocations,
    long TotalTokens,
    decimal TotalCostUsd,
    IReadOnlyList<AiUsageProjectRow> ByProject);

// Admin-only: aggregated AI spend for the last N days, grouped by project. Lets
// finance / leads see who's burning tokens at a glance.
public sealed record GetAiUsageQuery(int DaysWindow = 30) : IRequest<AiUsageDto>, IRequireRole
{
    public string RequiredRole => "Admin";
}

public sealed class GetAiUsageQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAiUsageQuery, AiUsageDto>
{
    public async Task<AiUsageDto> Handle(GetAiUsageQuery request, CancellationToken cancellationToken)
    {
        var days = Math.Clamp(request.DaysWindow, 1, 365);
        var since = DateTime.UtcNow.AddDays(-days);

        // Pull the raw rows for the window then aggregate in-memory. The dataset scales
        // with AI invocations in the last N days (default 30) — well under a query
        // plan's pathological size, and this way the query is identical on SQL Server
        // and the InMemory provider used in integration tests.
        var rows = await dbContext.AiInvocations
            .AsNoTracking()
            .Where(i => i.StartedAt >= since)
            .Select(i => new
            {
                i.ProjectId,
                ProjectName = i.Project != null ? i.Project.Name : null,
                i.TotalTokens,
                i.EstimatedCostUsd,
                i.Success,
            })
            .ToListAsync(cancellationToken);

        var perProject = rows
            .GroupBy(r => new { r.ProjectId, r.ProjectName })
            .Select(g => new AiUsageProjectRow(
                g.Key.ProjectId,
                g.Key.ProjectName,
                g.Count(),
                g.Sum(r => (long)r.TotalTokens),
                g.Sum(r => r.EstimatedCostUsd),
                g.Count(r => !r.Success)))
            .OrderByDescending(r => r.TotalCostUsd)
            .ToList();

        return new AiUsageDto(
            days,
            rows.Count,
            rows.Sum(r => (long)r.TotalTokens),
            rows.Sum(r => r.EstimatedCostUsd),
            perProject);
    }
}
