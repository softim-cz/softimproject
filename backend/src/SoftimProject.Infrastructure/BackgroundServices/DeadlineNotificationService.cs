using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Enums;

namespace SoftimProject.Infrastructure.BackgroundServices;

public sealed class DeadlineNotificationService(
    IServiceScopeFactory scopeFactory,
    IJobRegistry jobRegistry,
    ILogger<DeadlineNotificationService> logger)
    : TrackedBackgroundService(scopeFactory, jobRegistry, logger, TimeSpan.FromHours(1))
{
    protected override async Task ExecuteIterationAsync(
        IServiceProvider services,
        IJobRunScope run,
        CancellationToken cancellationToken)
    {
        // Hourly tick, but only do work in the ~08:00 CET slot — other hours exit immediately.
        if (DateTime.UtcNow.Hour != 7)
        {
            run.MarkSuccess(itemsProcessed: 0);
            return;
        }

        var dbContext = services.GetRequiredService<IApplicationDbContext>();
        var notificationService = services.GetRequiredService<INotificationService>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var warningDate = today.AddDays(3);

        var projectsNearDeadline = await dbContext.Projects
            .Where(p => p.Status == ProjectStatus.Active
                && p.DeadlineDate.HasValue
                && p.DeadlineDate.Value <= warningDate
                && p.DeadlineDate.Value >= today)
            .ToListAsync(cancellationToken);

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
                cancellationToken);
        }

        var ticketsNearDue = await dbContext.Tickets
            .Where(t => !t.TaskState.IsClosedState
                && t.DueDate.HasValue
                && t.DueDate.Value <= warningDate
                && t.DueDate.Value >= today
                && t.AssigneeId.HasValue)
            .ToListAsync(cancellationToken);

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
                cancellationToken);
        }

        logger.LogInformation(
            "Deadline notifications sent: {Projects} projects, {Tickets} tickets",
            projectsNearDeadline.Count, ticketsNearDue.Count);
        run.MarkSuccess(itemsProcessed: projectsNearDeadline.Count + ticketsNearDue.Count);
    }
}
