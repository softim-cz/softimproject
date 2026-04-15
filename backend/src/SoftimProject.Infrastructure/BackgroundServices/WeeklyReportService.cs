using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class WeeklyReportService(IServiceScopeFactory scopeFactory, ILogger<WeeklyReportService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = DateTime.UtcNow;
            if (now.DayOfWeek != DayOfWeek.Monday || now.Hour != 6) // Mon 7:00 CET
                continue;

            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();

                var activeProjects = await dbContext.Projects
                    .Where(p => p.Status == ProjectStatus.Active)
                    .ToListAsync(stoppingToken);

                var periodEnd = DateOnly.FromDateTime(now);
                var periodStart = periodEnd.AddDays(-7);

                foreach (var project in activeProjects)
                {
                    try
                    {
                        var worklogs = await dbContext.Worklogs
                            .Where(w => w.ProjectId == project.Id && w.Date >= periodStart && w.Date <= periodEnd)
                            .ToListAsync(stoppingToken);

                        var ticketsCreated = await dbContext.Tickets
                            .CountAsync(t => t.ProjectId == project.Id && t.CreatedAt >= now.AddDays(-7), stoppingToken);

                        var ticketsClosed = await dbContext.Tickets
                            .CountAsync(t => t.ProjectId == project.Id && t.TaskState.IsClosedState && t.UpdatedAt >= now.AddDays(-7), stoppingToken);

                        var data = $"""
                            Hours logged: {worklogs.Sum(w => w.Hours):F1}
                            Tickets created: {ticketsCreated}
                            Tickets completed: {ticketsClosed}
                            Budget hours remaining: {(project.BudgetHours ?? 0) - project.SpentHours:F1}
                            Health score: {project.HealthScore}/100
                            """;

                        var (report, tokensUsed) = await aiService.GenerateReportAsync(
                            project.Name, "Weekly Status", $"{periodStart:d} to {periodEnd:d}", data, stoppingToken);

                        dbContext.AiReports.Add(new AiReport
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = project.Id,
                            ReportType = AiReportType.WeeklyStatus,
                            PeriodStart = periodStart,
                            PeriodEnd = periodEnd,
                            Content = report,
                            TokensUsed = tokensUsed,
                            GeneratedAt = DateTime.UtcNow
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to generate weekly report for project {ProjectCode}", project.Code);
                    }
                }

                await dbContext.SaveChangesAsync(stoppingToken);
                logger.LogInformation("Weekly reports generated for {Count} projects", activeProjects.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Weekly report service failed");
            }
        }
    }
}
