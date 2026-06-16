using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Worklogs.CreateWorklogsBatch;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.Infrastructure.Tests;

public class CreateWorklogsBatchTests
{
    [Fact]
    public async Task Creates_all_entries_and_recalculates_cumulative_hours()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var handler = new CreateWorklogsBatchCommandHandler(db, MockUser(seed.OwnerId, isAdmin: false).Object);

        var ids = await handler.Handle(
            new CreateWorklogsBatchCommand(
                seed.ProjectId,
                seed.TicketId,
                new[]
                {
                    new CreateWorklogsBatchItem(new DateOnly(2026, 6, 1), 2m, "first batch entry", true),
                    new CreateWorklogsBatchItem(new DateOnly(2026, 6, 2), 3.5m, "second batch entry", false),
                }),
            CancellationToken.None);

        ids.Should().HaveCount(2);
        var worklogs = await db.Worklogs.Where(w => w.TicketId == seed.TicketId).ToListAsync();
        worklogs.Should().HaveCount(2);
        worklogs.Should().OnlyContain(w => w.UserId == seed.OwnerId && w.Source == WorklogSource.Manual);

        var ticket = await db.Tickets.FirstAsync(t => t.Id == seed.TicketId);
        ticket.CumulativeWorkedHours.Should().Be(5.5m);
    }

    [Fact]
    public async Task Non_admin_cannot_log_on_behalf_of_another_user()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var handler = new CreateWorklogsBatchCommandHandler(db, MockUser(seed.OwnerId, isAdmin: false).Object);

        var act = () => handler.Handle(
            new CreateWorklogsBatchCommand(
                seed.ProjectId,
                seed.TicketId,
                new[] { new CreateWorklogsBatchItem(new DateOnly(2026, 6, 1), 1m, "behalf of another", true) },
                OverrideUserId: Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Unknown_ticket_throws_not_found()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var handler = new CreateWorklogsBatchCommandHandler(db, MockUser(seed.OwnerId, isAdmin: false).Object);

        var act = () => handler.Handle(
            new CreateWorklogsBatchCommand(
                seed.ProjectId,
                Guid.NewGuid(),
                new[] { new CreateWorklogsBatchItem(new DateOnly(2026, 6, 1), 1m, "unknown ticket entry", true) }),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
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

    private static async Task<(Guid OwnerId, Guid ProjectId, Guid TicketId)> SeedAsync(ApplicationDbContext db)
    {
        var owner = new User { Id = Guid.NewGuid(), EntraObjectId = Guid.NewGuid().ToString(), Email = "o@x.local", DisplayName = "Owner", GlobalRole = GlobalRole.User, IsActive = true, CreatedAt = DateTime.UtcNow };
        var template = new ProjectTemplate { Id = Guid.NewGuid(), Name = $"T-{Guid.NewGuid():N}", IsActive = true, CreatedAt = DateTime.UtcNow };
        var project = new Project { Id = Guid.NewGuid(), Name = "P", Code = "PRJ", Status = ProjectStatus.Active, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };
        var state = new TaskState { Id = Guid.NewGuid(), Name = "Todo", Color = "#fff", SortOrder = 1, IsActive = true, IsDefault = true, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };
        var prio = new TicketPriority { Id = Guid.NewGuid(), Name = "Normal", Color = "#0f0", SortOrder = 1, IsActive = true, IsDefault = true, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };
        var ticket = new Ticket { Id = Guid.NewGuid(), ProjectId = project.Id, Project = project, Number = 1, Title = "T1", TicketPriorityId = prio.Id, TicketPriority = prio, TaskStateId = state.Id, TaskState = state, Position = 1, ReporterId = owner.Id, Reporter = owner, EstimatedHours = 4m, CreatedAt = DateTime.UtcNow };

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
