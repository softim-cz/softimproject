using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;
using SoftimProject.Application.Features.Worklogs.AiHistory;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.Infrastructure.Tests;

public class ResummarizeWorklogTests
{
    [Fact]
    public async Task Stores_ai_summary_on_worklog()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var worklog = AddWorklog(db, seed.TicketId, seed.OwnerId);
        await db.SaveChangesAsync();

        var ai = new Mock<IAiService>();
        ai.Setup(a => a.SummarizeWorklogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("Opravena chyba v přihlášení.", new AiTokenUsage(20, 8), "prompt"));

        var handler = new ResummarizeWorklogCommandHandler(db, ai.Object, PassThroughRecorder(), MockUser(seed.OwnerId).Object);

        var invocationId = await handler.Handle(
            new ResummarizeWorklogCommand(seed.ProjectId, worklog.Id), CancellationToken.None);

        invocationId.Should().NotBeEmpty();
        var updated = await db.Worklogs.FirstAsync(w => w.Id == worklog.Id);
        updated.AiSummary.Should().Be("Opravena chyba v přihlášení.");
    }

    [Fact]
    public async Task Empty_ai_output_throws_and_does_not_store()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var worklog = AddWorklog(db, seed.TicketId, seed.OwnerId);
        await db.SaveChangesAsync();

        var ai = new Mock<IAiService>();
        ai.Setup(a => a.SummarizeWorklogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string.Empty, new AiTokenUsage(0, 0), "prompt"));

        var handler = new ResummarizeWorklogCommandHandler(db, ai.Object, PassThroughRecorder(), MockUser(seed.OwnerId).Object);

        var act = () => handler.Handle(
            new ResummarizeWorklogCommand(seed.ProjectId, worklog.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        var updated = await db.Worklogs.FirstAsync(w => w.Id == worklog.Id);
        updated.AiSummary.Should().BeNull();
    }

    // Recorder stub that just runs the provided call and returns its payload, so the
    // handler logic (store/throw) can be tested without the real audit/rate-limit path.
    private static IAiInvocationRecorder PassThroughRecorder()
    {
        var mock = new Mock<IAiInvocationRecorder>();
        mock.Setup(r => r.RecordAsync(
                It.IsAny<AiInvocationContext>(),
                It.IsAny<Func<CancellationToken, Task<AiInvocationCall<string>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (AiInvocationContext _, Func<CancellationToken, Task<AiInvocationCall<string>>> call, CancellationToken ct) =>
            {
                var result = await call(ct);
                return new AiInvocationResult<string>(result.Payload, Guid.NewGuid());
            });
        return mock.Object;
    }

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

    private static Worklog AddWorklog(ApplicationDbContext db, Guid ticketId, Guid userId)
    {
        var worklog = new Worklog
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UserId = userId,
            Hours = 2m,
            Date = new DateOnly(2026, 6, 1),
            Description = "Opraveno přihlašování přes Entra, doplněn chybový stav.",
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
        var ticket = new Ticket { Id = Guid.NewGuid(), ProjectId = project.Id, Number = 1, Title = "Přihlášení", TicketPriorityId = prio.Id, TaskStateId = state.Id, Position = 1, ReporterId = owner.Id, CreatedAt = DateTime.UtcNow };

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
