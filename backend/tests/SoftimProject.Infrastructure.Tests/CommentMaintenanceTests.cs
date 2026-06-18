using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SoftimProject.Application.Features.Comments.DeleteComment;
using SoftimProject.Application.Features.Comments.UpdateComment;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.Infrastructure.Tests;

public class CommentMaintenanceTests
{
    [Fact]
    public async Task Deleting_latest_comment_falls_back_to_previous_for_LastComment()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        AddComment(db, seed, "older comment", DateTime.UtcNow);
        var newer = AddComment(db, seed, "newer comment", DateTime.UtcNow.AddMinutes(1));
        await SetLastComment(db, seed.TicketId, "newer comment");

        var handler = new DeleteCommentCommandHandler(db, MockUser(seed.OwnerId, isAdmin: false).Object);

        await handler.Handle(
            new DeleteCommentCommand(seed.ProjectId, seed.TicketId, newer.Id),
            CancellationToken.None);

        (await db.Comments.AnyAsync(c => c.Id == newer.Id)).Should().BeFalse();
        var ticket = await db.Tickets.FirstAsync(t => t.Id == seed.TicketId);
        ticket.LastComment.Should().Be("older comment");
    }

    [Fact]
    public async Task Deleting_only_comment_clears_LastComment()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var only = AddComment(db, seed, "only comment", DateTime.UtcNow);
        await SetLastComment(db, seed.TicketId, "only comment");

        var handler = new DeleteCommentCommandHandler(db, MockUser(seed.OwnerId, isAdmin: false).Object);

        await handler.Handle(
            new DeleteCommentCommand(seed.ProjectId, seed.TicketId, only.Id),
            CancellationToken.None);

        var ticket = await db.Tickets.FirstAsync(t => t.Id == seed.TicketId);
        ticket.LastComment.Should().BeNull();
    }

    [Fact]
    public async Task Updating_latest_comment_updates_LastComment()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db);
        var comment = AddComment(db, seed, "original comment", DateTime.UtcNow);
        await SetLastComment(db, seed.TicketId, "original comment");

        var handler = new UpdateCommentCommandHandler(db, MockUser(seed.OwnerId, isAdmin: false).Object);

        await handler.Handle(
            new UpdateCommentCommand(seed.ProjectId, seed.TicketId, comment.Id, "edited comment"),
            CancellationToken.None);

        var ticket = await db.Tickets.FirstAsync(t => t.Id == seed.TicketId);
        ticket.LastComment.Should().Be("edited comment");
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

    private static Comment AddComment(ApplicationDbContext db, SeedData seed, string content, DateTime createdAt)
    {
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            ProjectId = seed.ProjectId,
            TicketId = seed.TicketId,
            AuthorId = seed.OwnerId,
            Content = content,
            IsInternal = false,
            Source = CommentSource.Manual,
            CreatedAt = createdAt,
        };
        db.Comments.Add(comment);
        db.SaveChanges();
        return comment;
    }

    private static async Task SetLastComment(ApplicationDbContext db, Guid ticketId, string value)
    {
        var ticket = await db.Tickets.FirstAsync(t => t.Id == ticketId);
        ticket.LastComment = value;
        await db.SaveChangesAsync();
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
        return new SeedData(owner.Id, project.Id, ticket.Id);
    }

    private sealed record SeedData(Guid OwnerId, Guid ProjectId, Guid TicketId);
}
