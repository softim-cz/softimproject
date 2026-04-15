using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.Comments.CreateComment;
using SoftimProject.Application.Features.Worklogs.CreateWorklog;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;
using SoftimProject.Infrastructure.Services;

namespace SoftimProject.Infrastructure.Tests;

public class OwnershipAndAiTests
{
    [Fact]
    public async Task CreateComment_Should_Reject_Ticket_From_Different_Project()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProjectGraphAsync(dbContext);
        var currentUser = MockCurrentUser(seed.User.Id);
        var handler = new CreateCommentCommandHandler(dbContext, currentUser.Object);

        var act = () => handler.Handle(
            new CreateCommentCommand(seed.OtherProject.Id, seed.Ticket.Id, "Blocked", true),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateComment_Should_Set_ProjectId_And_Update_LastComment()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProjectGraphAsync(dbContext);
        var currentUser = MockCurrentUser(seed.User.Id);
        var handler = new CreateCommentCommandHandler(dbContext, currentUser.Object);

        var commentId = await handler.Handle(
            new CreateCommentCommand(seed.Project.Id, seed.Ticket.Id, "Latest customer update", true),
            CancellationToken.None);

        var comment = await dbContext.Comments.SingleAsync(c => c.Id == commentId);
        var ticket = await dbContext.Tickets.SingleAsync(t => t.Id == seed.Ticket.Id);

        comment.ProjectId.Should().Be(seed.Project.Id);
        ticket.LastComment.Should().Be("Latest customer update");
    }

    [Fact]
    public async Task CreateWorklog_Should_Reject_Ticket_From_Different_Project()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedProjectGraphAsync(dbContext);
        var currentUser = MockCurrentUser(seed.User.Id);
        var handler = new CreateWorklogCommandHandler(dbContext, currentUser.Object);

        var act = () => handler.Handle(
            new CreateWorklogCommand(seed.OtherProject.Id, seed.Ticket.Id, new DateOnly(2026, 3, 5), 2m, "Review", true),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AiService_Should_Return_Empty_Summary_When_Not_Configured()
    {
        var configuration = new ConfigurationBuilder().Build();
        var service = new AiService(configuration);

        var (summary, tokensUsed) = await service.SummarizeTicketAsync(
            "Ticket",
            "Description",
            ["Comment"]);

        summary.Should().BeEmpty();
        tokensUsed.Should().Be(0);
    }

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

    private static async Task<(User User, ProjectTemplate Template, Project Project, Project OtherProject, Ticket Ticket)> SeedProjectGraphAsync(ApplicationDbContext dbContext)
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
            Description = "Test template",
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
            IsClosedState = false,
            ProjectTemplateId = template.Id,
            ProjectTemplate = template,
            CreatedAt = DateTime.UtcNow,
        };

        var ticketPriority = new TicketPriority
        {
            Id = Guid.NewGuid(),
            Name = "Normal",
            Color = "#00ff00",
            SortOrder = 1,
            IsActive = true,
            IsDefault = true,
            ProjectTemplateId = template.Id,
            ProjectTemplate = template,
            CreatedAt = DateTime.UtcNow,
        };

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Main project",
            Code = "MAIN",
            Status = ProjectStatus.Active,
            ProjectTemplateId = template.Id,
            ProjectTemplate = template,
            CreatedAt = DateTime.UtcNow,
        };

        var otherProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Other project",
            Code = "OTHR",
            Status = ProjectStatus.Active,
            ProjectTemplateId = template.Id,
            ProjectTemplate = template,
            CreatedAt = DateTime.UtcNow,
        };

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Project = project,
            Number = 1,
            Title = "Investigate issue",
            TicketPriorityId = ticketPriority.Id,
            TicketPriority = ticketPriority,
            TaskStateId = taskState.Id,
            TaskState = taskState,
            Position = 1,
            ReporterId = user.Id,
            Reporter = user,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.Users.Add(user);
        dbContext.ProjectTemplates.Add(template);
        dbContext.TaskStates.Add(taskState);
        dbContext.TicketPriorities.Add(ticketPriority);
        dbContext.Projects.AddRange(project, otherProject);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        return (user, template, project, otherProject, ticket);
    }
}


