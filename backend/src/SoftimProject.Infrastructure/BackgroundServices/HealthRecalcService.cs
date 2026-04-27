using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class HealthRecalcService(
    IServiceScopeFactory scopeFactory,
    IJobRegistry jobRegistry,
    ILogger<HealthRecalcService> logger)
    : TrackedBackgroundService(scopeFactory, jobRegistry, logger, TimeSpan.FromHours(1))
{
    protected override async Task ExecuteIterationAsync(
        IServiceProvider services,
        IJobRunScope run,
        CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<IApplicationDbContext>();

        var projects = await dbContext.Projects
            .Where(p => p.Status == ProjectStatus.Active)
            .ToListAsync(cancellationToken);

        if (projects.Count == 0)
        {
            run.MarkSuccess(itemsProcessed: 0);
            return;
        }

        var projectIds = projects.Select(p => p.Id).ToList();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var staleThreshold = DateTime.UtcNow.AddDays(-7);

        var staleTicketCounts = await dbContext.Tickets
            .Where(t => projectIds.Contains(t.ProjectId)
                && !t.TaskState.IsClosedState
                && t.UpdatedAt < staleThreshold)
            .GroupBy(t => t.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count, cancellationToken);

        var spentHours = await dbContext.Worklogs
            .Where(w => projectIds.Contains(w.Ticket.ProjectId))
            .GroupBy(w => w.Ticket.ProjectId)
            .Select(g => new { ProjectId = g.Key, Total = g.Sum(w => w.Hours) })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Total, cancellationToken);

        var spentAmounts = await dbContext.Worklogs
            .Where(w => projectIds.Contains(w.Ticket.ProjectId) && w.IsBillable && w.HourlyRateSnapshot.HasValue)
            .GroupBy(w => w.Ticket.ProjectId)
            .Select(g => new { ProjectId = g.Key, Total = g.Sum(w => w.Hours * w.HourlyRateSnapshot!.Value) })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Total, cancellationToken);

        foreach (var project in projects)
        {
            var score = 100;

            if (project.BudgetHours.HasValue && project.BudgetHours > 0)
            {
                var budgetUsage = project.SpentHours / project.BudgetHours.Value;
                project.IsOverBudget = budgetUsage > 1.0m;
                if (budgetUsage > 1.0m) score -= 40;
                else if (budgetUsage > 0.9m) score -= 20;
                else if (budgetUsage > 0.8m) score -= 10;
            }

            if (project.DeadlineDate.HasValue)
            {
                var daysLeft = project.DeadlineDate.Value.DayNumber - today.DayNumber;
                project.IsOverDeadline = daysLeft < 0;
                if (daysLeft < 0) score -= 30;
                else if (daysLeft < 3) score -= 15;
                else if (daysLeft < 7) score -= 5;
            }

            var staleCount = staleTicketCounts.GetValueOrDefault(project.Id);
            if (staleCount > 5) score -= 30;
            else if (staleCount > 2) score -= 15;
            else if (staleCount > 0) score -= 5;

            project.SpentHours = spentHours.GetValueOrDefault(project.Id);
            project.SpentAmount = spentAmounts.GetValueOrDefault(project.Id);
            project.HealthScore = Math.Max(0, score);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        run.MarkSuccess(itemsProcessed: projects.Count);
    }
}
