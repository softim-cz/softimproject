using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Worklogs.CreateWorklog;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.Infrastructure.Tests;

public class CumulativeWorkedHoursTests
{
    [Fact]
    public async Task RecalculateProjectAsync_RollsUpHours_AcrossWholeSubtree()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedAsync(dbContext);

        // parent <- child <- grandchild, with own worklog hours 1 / 2 / 4.
        var parent = await AddTicketAsync(dbContext, seed, number: 1, parentId: null);
        var child = await AddTicketAsync(dbContext, seed, number: 2, parentId: parent.Id);
        var grandchild = await AddTicketAsync(dbContext, seed, number: 3, parentId: child.Id);
        await AddWorklogAsync(dbContext, seed, parent.Id, 1m);
        await AddWorklogAsync(dbContext, seed, child.Id, 2m);
        await AddWorklogAsync(dbContext, seed, grandchild.Id, 4m);

        await CumulativeWorkedHoursCalculator.RecalculateProjectAsync(dbContext, seed.Project.Id, CancellationToken.None);

        (await Hours(dbContext, grandchild.Id)).Should().Be(4m);
        (await Hours(dbContext, child.Id)).Should().Be(6m);
        (await Hours(dbContext, parent.Id)).Should().Be(7m);
    }

    [Fact]
    public async Task CreateWorklog_Propagates_CumulativeHours_UpTheParentChain()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedAsync(dbContext);
        var parent = await AddTicketAsync(dbContext, seed, number: 1, parentId: null);
        var child = await AddTicketAsync(dbContext, seed, number: 2, parentId: parent.Id);

        var handler = new CreateWorklogCommandHandler(dbContext, MockCurrentUser(seed.User.Id).Object);
        await handler.Handle(
            new CreateWorklogCommand(seed.Project.Id, child.Id, new DateOnly(2026, 6, 11), 3m, "Implementing the sub-task.", true),
            CancellationToken.None);

        (await Hours(dbContext, child.Id)).Should().Be(3m);
        (await Hours(dbContext, parent.Id)).Should().Be(3m);
    }

    [Fact]
    public async Task RecalculateUpward_AfterReparent_MovesHoursBetweenBranches()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedAsync(dbContext);
        var oldParent = await AddTicketAsync(dbContext, seed, number: 1, parentId: null);
        var newParent = await AddTicketAsync(dbContext, seed, number: 2, parentId: null);
        var moving = await AddTicketAsync(dbContext, seed, number: 3, parentId: oldParent.Id);
        await AddWorklogAsync(dbContext, seed, moving.Id, 5m);
        await CumulativeWorkedHoursCalculator.RecalculateProjectAsync(dbContext, seed.Project.Id, CancellationToken.None);

        (await Hours(dbContext, oldParent.Id)).Should().Be(5m);
        (await Hours(dbContext, newParent.Id)).Should().Be(0m);

        // Re-parent the moving ticket, then recompute both ancestor chains.
        moving.ParentTicketId = newParent.Id;
        await dbContext.SaveChangesAsync();
        await CumulativeWorkedHoursCalculator.RecalculateUpwardAsync(dbContext, oldParent.Id, CancellationToken.None);
        await CumulativeWorkedHoursCalculator.RecalculateUpwardAsync(dbContext, newParent.Id, CancellationToken.None);

        (await Hours(dbContext, oldParent.Id)).Should().Be(0m);
        (await Hours(dbContext, newParent.Id)).Should().Be(5m);
    }

    private static async Task<decimal> Hours(ApplicationDbContext dbContext, Guid ticketId) =>
        (await dbContext.Tickets.AsNoTracking().SingleAsync(t => t.Id == ticketId)).CumulativeWorkedHours;

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Mock<ICurrentUserService> MockCurrentUser(Guid userId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(x => x.UserId).Returns(userId);
        return mock;
    }

    private static async Task<Ticket> AddTicketAsync(
        ApplicationDbContext dbContext, SeedData seed, int number, Guid? parentId)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = seed.Project.Id,
            Number = number,
            Title = $"Ticket {number}",
            TicketPriorityId = seed.PriorityId,
            TaskStateId = seed.TaskStateId,
            ParentTicketId = parentId,
            Position = number,
            ReporterId = seed.User.Id,
            CreatedAt = DateTime.UtcNow,
        };
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();
        return ticket;
    }

    private static async Task AddWorklogAsync(ApplicationDbContext dbContext, SeedData seed, Guid ticketId, decimal hours)
    {
        dbContext.Worklogs.Add(new Worklog
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UserId = seed.User.Id,
            Date = new DateOnly(2026, 6, 11),
            Hours = hours,
            Description = "Worklog entry for cumulative test.",
            Source = WorklogSource.Manual,
            IsBillable = true,
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();
    }

    private sealed record SeedData(User User, Project Project, Guid TaskStateId, Guid PriorityId);

    private static async Task<SeedData> SeedAsync(ApplicationDbContext dbContext)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            EntraObjectId = Guid.NewGuid().ToString(),
            Email = "tester@softim.local",
            DisplayName = "Test User",
            GlobalRole = GlobalRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var template = new ProjectTemplate
        {
            Id = Guid.NewGuid(),
            Name = $"Template-{Guid.NewGuid():N}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var taskState = new TaskState
        {
            Id = Guid.NewGuid(),
            Name = "Todo",
            Color = "#0000ff",
            SortOrder = 1,
            IsActive = true,
            IsDefault = true,
            ProjectTemplateId = template.Id,
            CreatedAt = DateTime.UtcNow,
        };
        var priority = new TicketPriority
        {
            Id = Guid.NewGuid(),
            Name = "Normal",
            Color = "#00ff00",
            SortOrder = 1,
            IsActive = true,
            IsDefault = true,
            ProjectTemplateId = template.Id,
            CreatedAt = DateTime.UtcNow,
        };
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Main project",
            Code = "MAIN",
            Status = ProjectStatus.Active,
            ProjectTemplateId = template.Id,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.Users.Add(user);
        dbContext.ProjectTemplates.Add(template);
        dbContext.TaskStates.Add(taskState);
        dbContext.TicketPriorities.Add(priority);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        return new SeedData(user, project, taskState.Id, priority.Id);
    }
}
