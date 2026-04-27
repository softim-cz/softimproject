using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class WeeklyReportService(
    IServiceScopeFactory scopeFactory,
    IJobRegistry jobRegistry,
    ILogger<WeeklyReportService> logger)
    : TrackedBackgroundService(scopeFactory, jobRegistry, logger, TimeSpan.FromHours(1))
{
    protected override async Task ExecuteIterationAsync(
        IServiceProvider services,
        IJobRunScope run,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        // Hourly tick but work only on Mon ~07:00 CET (06:00 UTC).
        if (now.DayOfWeek != DayOfWeek.Monday || now.Hour != 6)
        {
            run.MarkSuccess(itemsProcessed: 0);
            return;
        }

        var dbContext = services.GetRequiredService<IApplicationDbContext>();
        var aiService = services.GetRequiredService<IAiService>();
        var recorder = services.GetRequiredService<IAiInvocationRecorder>();

        var activeProjects = await dbContext.Projects
            .Where(p => p.Status == ProjectStatus.Active)
            .ToListAsync(cancellationToken);

        var periodEnd = DateOnly.FromDateTime(now);
        var periodStart = periodEnd.AddDays(-7);

        var processed = 0;
        var failed = 0;
        foreach (var project in activeProjects)
        {
            try
            {
                var worklogs = await dbContext.Worklogs
                    .Where(w => w.Ticket.ProjectId == project.Id && w.Date >= periodStart && w.Date <= periodEnd)
                    .ToListAsync(cancellationToken);

                var ticketsCreated = await dbContext.Tickets
                    .CountAsync(t => t.ProjectId == project.Id && t.CreatedAt >= now.AddDays(-7), cancellationToken);

                var ticketsClosed = await dbContext.Tickets
                    .CountAsync(t => t.ProjectId == project.Id && t.TaskState.IsClosedState && t.UpdatedAt >= now.AddDays(-7), cancellationToken);

                var data = $"""
                    Hours logged: {worklogs.Sum(w => w.Hours):F1}
                    Tickets created: {ticketsCreated}
                    Tickets completed: {ticketsClosed}
                    Budget hours remaining: {(project.BudgetHours ?? 0) - project.SpentHours:F1}
                    Health score: {project.HealthScore}/100
                    """;

                var recorded = await recorder.RecordAsync(
                    new AiInvocationContext(
                        AiInvocationTrigger.WeeklyReport,
                        InputText: $"project:{project.Id}|period:{periodStart:O}/{periodEnd:O}",
                        TriggeredByUserId: null,
                        ProjectId: project.Id,
                        TicketId: null),
                    async ct =>
                    {
                        var (report, usage, _) = await aiService.GenerateReportAsync(
                            project.Name, "Weekly Status", $"{periodStart:d} to {periodEnd:d}", data, ct);
                        return new AiInvocationCall<(string, int)>((report, usage.TotalTokens),
                            usage.PromptTokens, usage.CompletionTokens, report);
                    },
                    cancellationToken);

                var (reportText, tokensUsed) = recorded.Payload;
                dbContext.AiReports.Add(new AiReport
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    ReportType = AiReportType.WeeklyStatus,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    Content = reportText,
                    TokensUsed = tokensUsed,
                    GeneratedAt = DateTime.UtcNow
                });
                processed++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to generate weekly report for project {ProjectCode}", project.Code);
                failed++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Weekly reports generated for {Count} projects", processed);
        run.MarkSuccess(processed, failed);
    }
}
