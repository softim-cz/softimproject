using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Worklogs.UpdateWorklog;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.Infrastructure.Tests;

public class UpdateWorklogTests
{
    [Fact]
    public async Task Changing_hours_recalculates_cumulative_hours()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var worklog = AddWorklog(db, seed.TicketId, seed.OwnerId, 2m);
        await db.SaveChangesAsync();
        await CumulativeWorkedHoursCalculator.RecalculateUpwardAsync(db, seed.TicketId, CancellationToken.None);

        var handler = new UpdateWorklogCommandHandler(db, MockUser(seed.OwnerId, isAdmin: false).Object);

        await handler.Handle(
            new UpdateWorklogCommand(
                seed.ProjectId,
                worklog.Id,
                seed.TicketId,
                new DateOnly(2026, 6, 1),
                5m,
                "updated worklog entry",
                IsBillable: true,
                Invoiced: null),
            CancellationToken.None);

        var ticket = await db.Tickets.FirstAsync(t => t.Id == seed.TicketId);
        ticket.CumulativeWorkedHours.Should().Be(5m);
    }

    [Fact]
    public async Task Reanchoring_to_another_ticket_moves_hours_between_both()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var otherTicket = AddTicket(db, seed.ProjectId, seed.OwnerId, seed.TaskStateId, seed.PriorityId, number: 2);
        var worklog = AddWorklog(db, seed.TicketId, seed.OwnerId, 3m);
        await db.SaveChangesAsync();
        await CumulativeWorkedHoursCalculator.RecalculateUpwardAsync(db, seed.TicketId, CancellationToken.None);

        var handler = new UpdateWorklogCommandHandler(db, MockUser(seed.OwnerId, isAdmin: false).Object);

        await handler.Handle(
            new UpdateWorklogCommand(
                seed.ProjectId,
                worklog.Id,
                otherTicket.Id,
                new DateOnly(2026, 6, 1),
                3m,
                "moved worklog entry",
                IsBillable: true,
                Invoiced: null),
            CancellationToken.None);

        var source = await db.Tickets.FirstAsync(t => t.Id == seed.TicketId);
        var target = await db.Tickets.FirstAsync(t => t.Id == otherTicket.Id);
        source.CumulativeWorkedHours.Should().Be(0m);
        target.CumulativeWorkedHours.Should().Be(3m);
    }

    [Fact]
    public async Task Non_admin_cannot_reassign_worklog_to_another_user()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var worklog = AddWorklog(db, seed.TicketId, seed.OwnerId, 1m);
        await db.SaveChangesAsync();

        var handler = new UpdateWorklogCommandHandler(db, MockUser(seed.OwnerId, isAdmin: false).Object);

        var act = () => handler.Handle(
            new UpdateWorklogCommand(
                seed.ProjectId,
                worklog.Id,
                seed.TicketId,
                new DateOnly(2026, 6, 1),
                1m,
                "reassign worklog entry",
                IsBillable: true,
                Invoiced: null,
                OverrideUserId: Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Mock<ICurrentUserService> MockUser(Guid userId, bool isAdmin)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(x => x.UserId).Returns(userId);
        mock.Setup(x => x.IsInRole("Admin")).Returns(isAdmin);
        return mock;
    }

    private static Worklog AddWorklog(ApplicationDbContext db, Guid ticketId, Guid userId, decimal hours)
    {
        var worklog = new Worklog
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UserId = userId,
            Hours = hours,
            Date = new DateOnly(2026, 6, 1),
            Description = "seed worklog entry",
            IsBillable = true,
            Source = WorklogSource.Manual,
            CreatedAt = DateTime.UtcNow,
        };
        db.Worklogs.Add(worklog);
        return worklog;
    }

    private static Ticket AddTicket(
        ApplicationDbContext db, Guid projectId, Guid ownerId, Guid stateId, Guid priorityId, int number)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Number = number,
            Title = $"T{number}",
            TicketPriorityId = priorityId,
            TaskStateId = stateId,
            Position = number,
            ReporterId = ownerId,
            CreatedAt = DateTime.UtcNow,
        };
        db.Tickets.Add(ticket);
        return ticket;
    }

    private static async Task<SeedData> SeedAsync(ApplicationDbContext db)
    {
        var owner = new User { Id = Guid.NewGuid(), EntraObjectId = Guid.NewGuid().ToString(), Email = "o@x.local", DisplayName = "Owner", GlobalRole = GlobalRole.User, IsActive = true, CreatedAt = DateTime.UtcNow };
        var template = new ProjectTemplate { Id = Guid.NewGuid(), Name = $"T-{Guid.NewGuid():N}", IsActive = true, CreatedAt = DateTime.UtcNow };
        var project = new Project { Id = Guid.NewGuid(), Name = "P", Code = "PRJ", Status = ProjectStatus.Active, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };
        var state = new TaskState { Id = Guid.NewGuid(), Name = "Todo", Color = "#fff", SortOrder = 1, IsActive = true, IsDefault = true, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };
        var prio = new TicketPriority { Id = Guid.NewGuid(), Name = "Normal", Color = "#0f0", SortOrder = 1, IsActive = true, IsDefault = true, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };
        var ticket = new Ticket { Id = Guid.NewGuid(), ProjectId = project.Id, Number = 1, Title = "T1", TicketPriorityId = prio.Id, TaskStateId = state.Id, Position = 1, ReporterId = owner.Id, CreatedAt = DateTime.UtcNow };

        db.Users.Add(owner);
        db.ProjectTemplates.Add(template);
        db.Projects.Add(project);
        db.TaskStates.Add(state);
        db.TicketPriorities.Add(prio);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        return new SeedData(owner.Id, project.Id, ticket.Id, state.Id, prio.Id);
    }

    private sealed record SeedData(Guid OwnerId, Guid ProjectId, Guid TicketId, Guid TaskStateId, Guid PriorityId);
}
