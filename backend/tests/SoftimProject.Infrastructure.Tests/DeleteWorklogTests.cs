using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Worklogs.DeleteWorklog;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.Infrastructure.Tests;

public class DeleteWorklogTests
{
    [Fact]
    public async Task Deleting_worklog_recalculates_cumulative_hours()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var keep = AddWorklog(db, seed.TicketId, seed.OwnerId, 3m);
        var remove = AddWorklog(db, seed.TicketId, seed.OwnerId, 2m);
        await db.SaveChangesAsync();
        await CumulativeWorkedHoursCalculator.RecalculateUpwardAsync(db, seed.TicketId, CancellationToken.None);

        var handler = new DeleteWorklogCommandHandler(db, MockUser(seed.OwnerId, isAdmin: false).Object);

        await handler.Handle(new DeleteWorklogCommand(seed.ProjectId, remove.Id), CancellationToken.None);

        (await db.Worklogs.AnyAsync(w => w.Id == remove.Id)).Should().BeFalse();
        (await db.Worklogs.AnyAsync(w => w.Id == keep.Id)).Should().BeTrue();
        var ticket = await db.Tickets.FirstAsync(t => t.Id == seed.TicketId);
        ticket.CumulativeWorkedHours.Should().Be(3m);
    }

    [Fact]
    public async Task Non_owner_without_manager_role_cannot_delete()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var worklog = AddWorklog(db, seed.TicketId, seed.OwnerId, 1m);
        await db.SaveChangesAsync();

        // A different, non-manager, non-admin user.
        var handler = new DeleteWorklogCommandHandler(db, MockUser(Guid.NewGuid(), isAdmin: false).Object);

        var act = () => handler.Handle(new DeleteWorklogCommand(seed.ProjectId, worklog.Id), CancellationToken.None);

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

    private static async Task<(Guid OwnerId, Guid ProjectId, Guid TicketId)> SeedAsync(ApplicationDbContext db)
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
        return (owner.Id, project.Id, ticket.Id);
    }
}
