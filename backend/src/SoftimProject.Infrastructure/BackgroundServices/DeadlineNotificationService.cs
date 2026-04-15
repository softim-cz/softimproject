using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class DeadlineNotificationService(IServiceScopeFactory scopeFactory, ILogger<DeadlineNotificationService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (DateTime.UtcNow.Hour != 7) // Run at ~8:00 CET (7:00 UTC)
                continue;

            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var warningDate = today.AddDays(3);

                // Projects approaching deadline
                var projectsNearDeadline = await dbContext.Projects
                    .Where(p => p.Status == ProjectStatus.Active
                        && p.DeadlineDate.HasValue
                        && p.DeadlineDate.Value <= warningDate
                        && p.DeadlineDate.Value >= today)
                    .ToListAsync(stoppingToken);

                foreach (var project in projectsNearDeadline)
                {
                    var daysLeft = project.DeadlineDate!.Value.DayNumber - today.DayNumber;
                    await notificationService.SendToProjectAsync(
                        project.Id,
                        $"Deadline approaching: {project.Name}",
                        $"Project deadline is in {daysLeft} day(s) ({project.DeadlineDate.Value:d})",
                        NotificationType.DeadlineApproaching,
                        project.Id,
                        "Project",
                        stoppingToken);
                }

                // Tickets approaching due date
                var ticketsNearDue = await dbContext.Tickets
                    .Where(t => !t.TaskState.IsClosedState
                        && t.DueDate.HasValue
                        && t.DueDate.Value <= warningDate
                        && t.DueDate.Value >= today
                        && t.AssigneeId.HasValue)
                    .ToListAsync(stoppingToken);

                foreach (var ticket in ticketsNearDue)
                {
                    var daysLeft = ticket.DueDate!.Value.DayNumber - today.DayNumber;
                    await notificationService.SendAsync(
                        ticket.AssigneeId!.Value,
                        $"Due date approaching: {ticket.Title}",
                        $"Ticket is due in {daysLeft} day(s) ({ticket.DueDate.Value:d})",
                        NotificationType.DeadlineApproaching,
                        ticket.Id,
                        "Ticket",
                        stoppingToken);
                }

                logger.LogInformation("Deadline notifications sent: {Projects} projects, {Tickets} tickets",
                    projectsNearDeadline.Count, ticketsNearDue.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Deadline notification service failed");
            }
        }
    }
}
