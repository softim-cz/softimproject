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
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                var projects = await dbContext.Projects
                    .Where(p => p.Status == ProjectStatus.Active)
                    .ToListAsync(stoppingToken);

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

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

                    // Ticket health (30 points) - check for stale tickets
                    var staleTickets = await dbContext.Tickets
                        .CountAsync(t => t.ProjectId == project.Id
                            && t.Status == TicketStatus.InProgress
                            && t.UpdatedAt < DateTime.UtcNow.AddDays(-7), stoppingToken);

                    if (staleTickets > 5) score -= 30;
                    else if (staleTickets > 2) score -= 15;
                    else if (staleTickets > 0) score -= 5;

                    // Recalculate spent hours
                    project.SpentHours = await dbContext.Worklogs
                        .Where(w => w.ProjectId == project.Id)
                        .SumAsync(w => w.Hours, stoppingToken);

                    project.SpentAmount = await dbContext.Worklogs
                        .Where(w => w.ProjectId == project.Id && w.IsBillable && w.HourlyRateSnapshot.HasValue)
                        .SumAsync(w => w.Hours * w.HourlyRateSnapshot!.Value, stoppingToken);

                    project.HealthScore = Math.Max(0, score);
                }

                await dbContext.SaveChangesAsync(stoppingToken);
                logger.LogInformation("Health recalculated for {Count} projects", projects.Count);
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
