using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SoftimProject.Application.Features.Worklogs.GetWorklogs;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.Infrastructure.Tests;

public class GetWorklogsSubprojectTests
{
    [Fact]
    public async Task IncludeSubprojects_False_Returns_Only_Parent_Project_Worklogs()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedParentAndSubprojectAsync(dbContext);
        var handler = new GetWorklogsQueryHandler(dbContext, AdminUser(seed.UserId).Object);

        var result = await handler.Handle(
            new GetWorklogsQuery(ProjectId: seed.ParentProjectId, IncludeSubprojects: false),
            CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.Should().OnlyContain(w => w.ProjectId == seed.ParentProjectId);
    }

    [Fact]
    public async Task IncludeSubprojects_True_Returns_Parent_And_Descendant_Worklogs()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedParentAndSubprojectAsync(dbContext);
        var handler = new GetWorklogsQueryHandler(dbContext, AdminUser(seed.UserId).Object);

        var result = await handler.Handle(
            new GetWorklogsQuery(ProjectId: seed.ParentProjectId, IncludeSubprojects: true),
            CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Select(w => w.ProjectId)
            .Should().BeEquivalentTo(new[] { seed.ParentProjectId, seed.SubProjectId });
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Mock<ICurrentUserService> AdminUser(Guid userId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(x => x.UserId).Returns(userId);
        mock.Setup(x => x.IsInRole("Admin")).Returns(true);
        return mock;
    }

    private sealed record SeedData(Guid UserId, Guid ParentProjectId, Guid SubProjectId);

    private static async Task<SeedData> SeedParentAndSubprojectAsync(ApplicationDbContext dbContext)
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
        var parent = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Parent project",
            Code = "PAR",
            Status = ProjectStatus.Active,
            ProjectTemplateId = template.Id,
            CreatedAt = DateTime.UtcNow,
        };
        var sub = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Sub project",
            Code = "SUB",
            Status = ProjectStatus.Active,
            ProjectTemplateId = template.Id,
            ParentProjectId = parent.Id,
            CreatedAt = DateTime.UtcNow,
        };

        var parentTicket = NewTicket(parent.Id, 1, priority.Id, taskState.Id, user.Id);
        var subTicket = NewTicket(sub.Id, 1, priority.Id, taskState.Id, user.Id);

        dbContext.Users.Add(user);
        dbContext.ProjectTemplates.Add(template);
        dbContext.TaskStates.Add(taskState);
        dbContext.TicketPriorities.Add(priority);
        dbContext.Projects.AddRange(parent, sub);
        dbContext.Tickets.AddRange(parentTicket, subTicket);
        dbContext.Worklogs.AddRange(
            NewWorklog(parentTicket.Id, user.Id),
            NewWorklog(subTicket.Id, user.Id));
        await dbContext.SaveChangesAsync();

        return new SeedData(user.Id, parent.Id, sub.Id);
    }

    private static Ticket NewTicket(Guid projectId, int number, Guid priorityId, Guid taskStateId, Guid reporterId) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = projectId,
        Number = number,
        Title = $"Ticket {number}",
        TicketPriorityId = priorityId,
        TaskStateId = taskStateId,
        Position = number,
        ReporterId = reporterId,
        CreatedAt = DateTime.UtcNow,
    };

    private static Worklog NewWorklog(Guid ticketId, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        UserId = userId,
        Date = new DateOnly(2026, 6, 11),
        Hours = 1m,
        Description = "Worklog entry for subproject filter test.",
        Source = WorklogSource.Manual,
        IsBillable = true,
        CreatedAt = DateTime.UtcNow,
    };
}
