using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Worklogs.GetWorklogById;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.Infrastructure.Tests;

public class GetWorklogByIdTests
{
    [Fact]
    public async Task Owner_can_read_their_worklog()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var handler = new GetWorklogByIdQueryHandler(db, MockUser(seed.OwnerId, hasProjectAccess: false).Object);

        var dto = await handler.Handle(new GetWorklogByIdQuery(seed.WorklogId), CancellationToken.None);

        dto.Id.Should().Be(seed.WorklogId);
        dto.UserId.Should().Be(seed.OwnerId);
    }

    [Fact]
    public async Task Non_member_non_owner_gets_not_found()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var handler = new GetWorklogByIdQueryHandler(db, MockUser(Guid.NewGuid(), hasProjectAccess: false).Object);

        var act = () => handler.Handle(new GetWorklogByIdQuery(seed.WorklogId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Project_member_can_read_others_worklog()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var handler = new GetWorklogByIdQueryHandler(db, MockUser(Guid.NewGuid(), hasProjectAccess: true).Object);

        var dto = await handler.Handle(new GetWorklogByIdQuery(seed.WorklogId), CancellationToken.None);

        dto.Id.Should().Be(seed.WorklogId);
    }

    private static ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Mock<ICurrentUserService> MockUser(Guid userId, bool hasProjectAccess)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(x => x.UserId).Returns(userId);
        mock.Setup(x => x.IsInRole("Admin")).Returns(false);
        mock.Setup(x => x.HasProjectAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasProjectAccess);
        return mock;
    }

    private static async Task<(Guid OwnerId, Guid WorklogId)> SeedAsync(ApplicationDbContext db)
    {
        var owner = new User { Id = Guid.NewGuid(), EntraObjectId = Guid.NewGuid().ToString(), Email = "o@x.local", DisplayName = "Owner", GlobalRole = GlobalRole.User, IsActive = true, CreatedAt = DateTime.UtcNow };
        var template = new ProjectTemplate { Id = Guid.NewGuid(), Name = $"T-{Guid.NewGuid():N}", IsActive = true, CreatedAt = DateTime.UtcNow };
        var project = new Project { Id = Guid.NewGuid(), Name = "P", Code = "PRJ", Status = ProjectStatus.Active, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };
        var state = new TaskState { Id = Guid.NewGuid(), Name = "Todo", Color = "#fff", SortOrder = 1, IsActive = true, IsDefault = true, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };
        var prio = new TicketPriority { Id = Guid.NewGuid(), Name = "Normal", Color = "#0f0", SortOrder = 1, IsActive = true, IsDefault = true, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };
        var ticket = new Ticket { Id = Guid.NewGuid(), ProjectId = project.Id, Project = project, Number = 1, Title = "T1", TicketPriorityId = prio.Id, TicketPriority = prio, TaskStateId = state.Id, TaskState = state, Position = 1, ReporterId = owner.Id, Reporter = owner, CreatedAt = DateTime.UtcNow };
        var worklog = new Worklog { Id = Guid.NewGuid(), TicketId = ticket.Id, Ticket = ticket, UserId = owner.Id, User = owner, Date = new DateOnly(2026, 6, 1), Hours = 2m, Description = "work", Source = WorklogSource.Manual, IsBillable = true, CreatedAt = DateTime.UtcNow };

        db.Users.Add(owner);
        db.ProjectTemplates.Add(template);
        db.Projects.Add(project);
        db.TaskStates.Add(state);
        db.TicketPriorities.Add(prio);
        db.Tickets.Add(ticket);
        db.Worklogs.Add(worklog);
        await db.SaveChangesAsync();
        return (owner.Id, worklog.Id);
    }
}
