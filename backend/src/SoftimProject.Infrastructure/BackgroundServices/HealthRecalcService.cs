using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class HealthRecalcService(IServiceScopeFactory scopeFactory, ILogger<HealthRecalcService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var sw = Stopwatch.StartNew();

                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                var projects = await dbContext.Projects
                    .Where(p => p.Status == ProjectStatus.Active)
                    .ToListAsync(stoppingToken);

                if (projects.Count == 0) continue;

                var projectIds = projects.Select(p => p.Id).ToList();
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var staleThreshold = DateTime.UtcNow.AddDays(-7);

                // Batch: stale ticket counts per project (single query)
                var staleTicketCounts = await dbContext.Tickets
                    .Where(t => projectIds.Contains(t.ProjectId)
                        && !t.TaskState.IsClosedState
                        && t.UpdatedAt < staleThreshold)
                    .GroupBy(t => t.ProjectId)
                    .Select(g => new { ProjectId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ProjectId, x => x.Count, stoppingToken);

                // Batch: spent hours per project (single query)
                var spentHours = await dbContext.Worklogs
                    .Where(w => projectIds.Contains(w.ProjectId))
                    .GroupBy(w => w.ProjectId)
                    .Select(g => new { ProjectId = g.Key, Total = g.Sum(w => w.Hours) })
                    .ToDictionaryAsync(x => x.ProjectId, x => x.Total, stoppingToken);

                // Batch: spent amount per project (single query)
                var spentAmounts = await dbContext.Worklogs
                    .Where(w => projectIds.Contains(w.ProjectId) && w.IsBillable && w.HourlyRateSnapshot.HasValue)
                    .GroupBy(w => w.ProjectId)
                    .Select(g => new { ProjectId = g.Key, Total = g.Sum(w => w.Hours * w.HourlyRateSnapshot!.Value) })
                    .ToDictionaryAsync(x => x.ProjectId, x => x.Total, stoppingToken);

                foreach (var project in projects)
                {
                    var score = 100;

                    // Budget health (40 points)
                    if (project.BudgetHours.HasValue && project.BudgetHours > 0)
                    {
                        var budgetUsage = project.SpentHours / project.BudgetHours.Value;
                        project.IsOverBudget = budgetUsage > 1.0m;

                        if (budgetUsage > 1.0m) score -= 40;
                        else if (budgetUsage > 0.9m) score -= 20;
                        else if (budgetUsage > 0.8m) score -= 10;
                    }

                    // Deadline health (30 points)
                    if (project.DeadlineDate.HasValue)
                    {
                        var daysLeft = project.DeadlineDate.Value.DayNumber - today.DayNumber;
                        project.IsOverDeadline = daysLeft < 0;

                        if (daysLeft < 0) score -= 30;
                        else if (daysLeft < 3) score -= 15;
                        else if (daysLeft < 7) score -= 5;
                    }

                    // Ticket health (30 points)
                    var staleCount = staleTicketCounts.GetValueOrDefault(project.Id);
                    if (staleCount > 5) score -= 30;
                    else if (staleCount > 2) score -= 15;
                    else if (staleCount > 0) score -= 5;

                    project.SpentHours = spentHours.GetValueOrDefault(project.Id);
                    project.SpentAmount = spentAmounts.GetValueOrDefault(project.Id);
                    project.HealthScore = Math.Max(0, score);
                }

                await dbContext.SaveChangesAsync(stoppingToken);
                logger.LogInformation("Health recalculated for {Count} projects in {Ms}ms", projects.Count, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Health recalculation service failed");
            }
        }
    }
}
