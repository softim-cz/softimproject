using System.Text;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;
using SoftimProject.Application.Features.Worklogs.ImportWorklogs;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.Infrastructure.Tests;

public class ImportWorklogsTests
{
    [Fact]
    public async Task Imports_valid_rows_resolving_ticket_by_key_or_number()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var handler = new ImportWorklogsCommandHandler(db, MockUser(seed.OwnerId).Object);

        // ';' delimiter, ',' decimal separator, and both "PRJ-1" and bare "1" ticket refs.
        var csv = "ticket;date;hours;description\n"
            + "PRJ-1;2026-06-01;2;Work A done here\n"
            + "1;2026-06-02;1,5;Work B done here\n";

        var result = await handler.Handle(Command(seed.ProjectId, "import.csv", csv), CancellationToken.None);

        result.TotalRows.Should().Be(2);
        result.Created.Should().Be(2);
        result.Duplicates.Should().Be(0);
        result.Errors.Should().Be(0);

        var worklogs = await db.Worklogs.Where(w => w.TicketId == seed.TicketId).ToListAsync();
        worklogs.Should().HaveCount(2);
        worklogs.Should().OnlyContain(w => w.Source == WorklogSource.Import && w.UserId == seed.OwnerId);

        var ticket = await db.Tickets.FirstAsync(t => t.Id == seed.TicketId);
        ticket.CumulativeWorkedHours.Should().Be(3.5m);
    }

    [Fact]
    public async Task Skips_duplicates_against_existing_worklogs()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        db.Worklogs.Add(new Worklog
        {
            Id = Guid.NewGuid(),
            TicketId = seed.TicketId,
            UserId = seed.OwnerId,
            Date = new DateOnly(2026, 6, 1),
            Hours = 2m,
            Description = "Work A done here",
            Source = WorklogSource.Manual,
            IsBillable = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var handler = new ImportWorklogsCommandHandler(db, MockUser(seed.OwnerId).Object);

        var csv = "ticket;date;hours;description\n"
            + "PRJ-1;2026-06-01;2;Work A done here\n";

        var result = await handler.Handle(Command(seed.ProjectId, "import.csv", csv), CancellationToken.None);

        result.Created.Should().Be(0);
        result.Duplicates.Should().Be(1);
        result.Issues.Should().ContainSingle(i => i.Type == "Duplicate");
    }

    [Fact]
    public async Task Reports_unknown_ticket_as_error()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var handler = new ImportWorklogsCommandHandler(db, MockUser(seed.OwnerId).Object);

        var csv = "ticket;date;hours;description\n"
            + "PRJ-999;2026-06-01;2;Some description text\n";

        var result = await handler.Handle(Command(seed.ProjectId, "import.csv", csv), CancellationToken.None);

        result.Created.Should().Be(0);
        result.Errors.Should().Be(1);
        result.Issues.Should().ContainSingle(i => i.Type == "Error");
    }

    [Fact]
    public async Task Missing_required_column_throws_validation()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var handler = new ImportWorklogsCommandHandler(db, MockUser(seed.OwnerId).Object);

        // No "hours" column.
        var csv = "ticket;date;description\nPRJ-1;2026-06-01;Some description text\n";

        var act = () => handler.Handle(Command(seed.ProjectId, "import.csv", csv), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private static ImportWorklogsCommand Command(Guid projectId, string fileName, string csv) =>
        new(projectId, fileName, Encoding.UTF8.GetBytes(csv));

    private static ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Mock<ICurrentUserService> MockUser(Guid userId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(x => x.UserId).Returns(userId);
        mock.Setup(x => x.IsInRole("Admin")).Returns(false);
        return mock;
    }

    private static async Task<(Guid OwnerId, Guid ProjectId, Guid TicketId)> SeedAsync(ApplicationDbContext db)
    {
        var owner = new User { Id = Guid.NewGuid(), EntraObjectId = Guid.NewGuid().ToString(), Email = "o@x.local", DisplayName = "Owner", GlobalRole = GlobalRole.User, IsActive = true, CreatedAt = DateTime.UtcNow };
        var template = new ProjectTemplate { Id = Guid.NewGuid(), Name = $"T-{Guid.NewGuid():N}", IsActive = true, CreatedAt = DateTime.UtcNow };
        var project = new Project { Id = Guid.NewGuid(), Name = "P", Code = "PRJ", Status = ProjectStatus.Active, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };
        var state = new TaskState { Id = Guid.NewGuid(), Name = "Todo", Color = "#fff", SortOrder = 1, IsActive = true, IsDefault = true, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };
        var prio = new TicketPriority { Id = Guid.NewGuid(), Name = "Normal", Color = "#0f0", SortOrder = 1, IsActive = true, IsDefault = true, ProjectTemplateId = template.Id, ProjectTemplate = template, CreatedAt = DateTime.UtcNow };
        var ticket = new Ticket { Id = Guid.NewGuid(), ProjectId = project.Id, Project = project, Number = 1, Title = "T1", TicketPriorityId = prio.Id, TicketPriority = prio, TaskStateId = state.Id, TaskState = state, Position = 1, ReporterId = owner.Id, Reporter = owner, CreatedAt = DateTime.UtcNow };

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
