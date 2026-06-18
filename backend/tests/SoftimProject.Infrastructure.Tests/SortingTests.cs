using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SoftimProject.Application.Features.Tickets.GetTickets;
using SoftimProject.Application.Features.Worklogs.GetWorklogs;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.Infrastructure.Tests;

public class SortingTests
{
    [Fact]
    public async Task Tickets_sorted_by_title_ascending()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        AddTicket(db, seed, 1, "Beta");
        AddTicket(db, seed, 2, "Alpha");
        AddTicket(db, seed, 3, "Gamma");
        await db.SaveChangesAsync();

        var handler = new GetTicketsQueryHandler(db);
        var result = await handler.Handle(
            new GetTicketsQuery(seed.ProjectId, SortField: "title", SortDirection: "asc"),
            CancellationToken.None);

        result.Items.Select(t => t.Title).Should().ContainInOrder("Alpha", "Beta", "Gamma");
    }

    [Fact]
    public async Task Tickets_sorted_by_number_descending()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        AddTicket(db, seed, 1, "Beta");
        AddTicket(db, seed, 2, "Alpha");
        AddTicket(db, seed, 3, "Gamma");
        await db.SaveChangesAsync();

        var handler = new GetTicketsQueryHandler(db);
        var result = await handler.Handle(
            new GetTicketsQuery(seed.ProjectId, SortField: "key", SortDirection: "desc"),
            CancellationToken.None);

        result.Items.Select(t => t.Number).Should().ContainInOrder(3, 2, 1);
    }

    [Fact]
    public async Task Tickets_unknown_sort_field_falls_back_to_created_desc()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var first = AddTicket(db, seed, 1, "First");
        first.CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var second = AddTicket(db, seed, 2, "Second");
        second.CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        await db.SaveChangesAsync();

        var handler = new GetTicketsQueryHandler(db);
        var result = await handler.Handle(
            new GetTicketsQuery(seed.ProjectId, SortField: "doesNotExist"),
            CancellationToken.None);

        // Newest first.
        result.Items.Select(t => t.Number).Should().ContainInOrder(2, 1);
    }

    [Fact]
    public async Task Worklogs_sorted_by_hours_ascending()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var ticket = AddTicket(db, seed, 1, "T1");
        AddWorklog(db, ticket.Id, seed.OwnerId, 3m);
        AddWorklog(db, ticket.Id, seed.OwnerId, 1m);
        AddWorklog(db, ticket.Id, seed.OwnerId, 2m);
        await db.SaveChangesAsync();

        var handler = new GetWorklogsQueryHandler(db, MockUser(seed.OwnerId).Object);
        var result = await handler.Handle(
            new GetWorklogsQuery(ProjectId: seed.ProjectId, SortField: "hours", SortDirection: "asc"),
            CancellationToken.None);

        result.Items.Select(w => w.Hours).Should().ContainInOrder(1m, 2m, 3m);
    }

    private static ApplicationDbContext CreateDbContext() =>
        new(new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Mock<ICurrentUserService> MockUser(Guid userId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(x => x.UserId).Returns(userId);
        mock.Setup(x => x.IsInRole("Admin")).Returns(false);
        return mock;
    }

    private static Ticket AddTicket(ApplicationDbContext db, SeedData seed, int number, string title)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = seed.ProjectId,
            Number = number,
            Title = title,
            TicketPriorityId = seed.PriorityId,
            TaskStateId = seed.TaskStateId,
            Position = number,
            ReporterId = seed.OwnerId,
            CreatedAt = DateTime.UtcNow,
        };
        db.Tickets.Add(ticket);
        return ticket;
    }

    private static void AddWorklog(ApplicationDbContext db, Guid ticketId, Guid userId, decimal hours)
    {
        db.Worklogs.Add(new Worklog
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
        });
    }

    private static async Task<SeedData> SeedAsync(ApplicationDbContext db)
    {
        var owner = new User { Id = Guid.NewGuid(), EntraObjectId = Guid.NewGuid().ToString(), Email = "o@x.local", DisplayName = "Owner", GlobalRole = GlobalRole.User, IsActive = true, CreatedAt = DateTime.UtcNow };
        var template = new ProjectTemplate { Id = Guid.NewGuid(), Name = $"T-{Guid.NewGuid():N}", IsActive = true, CreatedAt = DateTime.UtcNow };
        var project = new Project { Id = Guid.NewGuid(), Name = "P", Code = "PRJ", Status = ProjectStatus.Active, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };
        var state = new TaskState { Id = Guid.NewGuid(), Name = "Todo", Color = "#fff", SortOrder = 1, IsActive = true, IsDefault = true, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };
        var prio = new TicketPriority { Id = Guid.NewGuid(), Name = "Normal", Color = "#0f0", SortOrder = 1, IsActive = true, IsDefault = true, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };

        db.Users.Add(owner);
        db.ProjectTemplates.Add(template);
        db.Projects.Add(project);
        db.TaskStates.Add(state);
        db.TicketPriorities.Add(prio);
        await db.SaveChangesAsync();
        return new SeedData(owner.Id, project.Id, state.Id, prio.Id);
    }

    private sealed record SeedData(Guid OwnerId, Guid ProjectId, Guid TaskStateId, Guid PriorityId);
}
