using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;
using SoftimProject.Infrastructure.Services.Email;

namespace SoftimProject.Infrastructure.Tests;

public class EmailSyncHelperTests
{
    [Fact]
    public async Task Sync_Creates_New_Ticket_When_Recipient_Matches_Alias()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db, externalProjectId: "acme");
        var mailbox = new FakeMailbox(
        [
            new EmailMessage(
                Id: "msg-1",
                Subject: "Server down",
                Body: "<p>Please help</p>",
                FromAddress: "client@acme.cz",
                FromDisplayName: "Client",
                ToRecipients: ["inbox+acme@softim.cz"],
                CcRecipients: [],
                ReceivedAt: DateTimeOffset.UtcNow),
        ]);

        var result = await EmailSyncHelper.SyncAsync(db, mailbox, "inbox+", 50, NullLogger.Instance, default);

        result.TotalSynced.Should().Be(1);
        result.TotalFailed.Should().Be(0);
        var ticket = await db.Tickets.SingleAsync();
        ticket.ProjectId.Should().Be(seed.Project.Id);
        ticket.Title.Should().Be("Server down");
        ticket.Description.Should().Be("Please help");
        ticket.ExternalId.Should().Be("msg-1");
        ticket.ExternalUser.Should().Be("client@acme.cz");
        ticket.Number.Should().Be(1);
        mailbox.MarkedRead.Should().BeEquivalentTo(["msg-1"]);
    }

    [Fact]
    public async Task Sync_Adds_Comment_When_Subject_References_Existing_Ticket()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db, externalProjectId: "acme");
        var existing = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = seed.Project.Id,
            Number = 42,
            Title = "Existing",
            TicketPriorityId = seed.PriorityId,
            TaskStateId = seed.StateId,
            ReporterId = seed.UserId,
            Position = 0,
            CreatedAt = DateTime.UtcNow,
        };
        db.Tickets.Add(existing);
        await db.SaveChangesAsync();

        var mailbox = new FakeMailbox(
        [
            new EmailMessage(
                Id: "msg-2",
                Subject: "Re: [#ACME-42] Server down",
                Body: "More info attached.",
                FromAddress: "client@acme.cz",
                FromDisplayName: null,
                ToRecipients: ["inbox+acme@softim.cz"],
                CcRecipients: [],
                ReceivedAt: DateTimeOffset.UtcNow),
        ]);

        var result = await EmailSyncHelper.SyncAsync(db, mailbox, "inbox+", 50, NullLogger.Instance, default);

        result.TotalSynced.Should().Be(1);
        var comment = await db.Comments.SingleAsync();
        comment.TicketId.Should().Be(existing.Id);
        comment.Source.Should().Be(CommentSource.Email);
        comment.ExternalId.Should().Be("msg-2");
        comment.Content.Should().Be("More info attached.");
        (await db.Tickets.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Sync_Skips_Email_When_Recipient_Does_Not_Match_Any_Project()
    {
        await using var db = CreateDbContext();
        await SeedAsync(db, externalProjectId: "acme");
        var mailbox = new FakeMailbox(
        [
            new EmailMessage(
                Id: "msg-3",
                Subject: "Hello",
                Body: "Body",
                FromAddress: "stranger@example.com",
                FromDisplayName: null,
                ToRecipients: ["info@softim.cz"],
                CcRecipients: [],
                ReceivedAt: DateTimeOffset.UtcNow),
        ]);

        var result = await EmailSyncHelper.SyncAsync(db, mailbox, "inbox+", 50, NullLogger.Instance, default);

        result.TotalSynced.Should().Be(0);
        (await db.Tickets.AnyAsync()).Should().BeFalse();
        // Unmatched emails are still marked read so they don't bombard logs every 2 minutes.
        mailbox.MarkedRead.Should().BeEquivalentTo(["msg-3"]);
    }

    [Fact]
    public async Task Sync_Is_Idempotent_On_ExternalId()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db, externalProjectId: "acme");
        db.Tickets.Add(new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = seed.Project.Id,
            Number = 1,
            Title = "Already imported",
            TicketPriorityId = seed.PriorityId,
            TaskStateId = seed.StateId,
            ReporterId = seed.UserId,
            ExternalId = "msg-dup",
            Position = 0,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var mailbox = new FakeMailbox(
        [
            new EmailMessage(
                Id: "msg-dup",
                Subject: "Duplicate import",
                Body: "Body",
                FromAddress: "x@y.cz",
                FromDisplayName: null,
                ToRecipients: ["inbox+acme@softim.cz"],
                CcRecipients: [],
                ReceivedAt: DateTimeOffset.UtcNow),
        ]);

        var result = await EmailSyncHelper.SyncAsync(db, mailbox, "inbox+", 50, NullLogger.Instance, default);

        result.TotalSynced.Should().Be(0);
        (await db.Tickets.CountAsync()).Should().Be(1);
        mailbox.MarkedRead.Should().BeEquivalentTo(["msg-dup"]);
    }

    [Fact]
    public async Task Sync_Skips_Graph_Call_When_No_Email_Projects_Configured()
    {
        await using var db = CreateDbContext();
        // No projects with ExternalSystem=Email — should bail out before touching mailbox.
        var mailbox = new FakeMailbox([]);

        var result = await EmailSyncHelper.SyncAsync(db, mailbox, "inbox+", 50, NullLogger.Instance, default);

        result.TotalSynced.Should().Be(0);
        mailbox.FetchCount.Should().Be(0);
    }

    [Fact]
    public async Task Sync_Falls_Back_To_New_Ticket_When_Reply_Token_References_Unknown_Number()
    {
        await using var db = CreateDbContext();
        var seed = await SeedAsync(db, externalProjectId: "acme");
        var mailbox = new FakeMailbox(
        [
            new EmailMessage(
                Id: "msg-fallback",
                Subject: "Re: [#ACME-999] Ghost ticket",
                Body: "Body",
                FromAddress: "client@acme.cz",
                FromDisplayName: null,
                ToRecipients: ["inbox+acme@softim.cz"],
                CcRecipients: [],
                ReceivedAt: DateTimeOffset.UtcNow),
        ]);

        var result = await EmailSyncHelper.SyncAsync(db, mailbox, "inbox+", 50, NullLogger.Instance, default);

        result.TotalSynced.Should().Be(1);
        (await db.Tickets.CountAsync()).Should().Be(1);
        (await db.Comments.AnyAsync()).Should().BeFalse();
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class FakeMailbox(IEnumerable<EmailMessage> messages) : IEmailMailboxClient
    {
        private readonly List<EmailMessage> _messages = [.. messages];
        public List<string> MarkedRead { get; } = [];
        public int FetchCount { get; private set; }

        public Task<IReadOnlyList<EmailMessage>> FetchUnreadAsync(int take, CancellationToken cancellationToken)
        {
            FetchCount++;
            return Task.FromResult<IReadOnlyList<EmailMessage>>(_messages.Take(take).ToList());
        }

        public Task MarkAsReadAsync(string messageId, CancellationToken cancellationToken)
        {
            MarkedRead.Add(messageId);
            return Task.CompletedTask;
        }
    }

    private static async Task<(Project Project, Guid UserId, Guid StateId, Guid PriorityId)> SeedAsync(
        ApplicationDbContext db, string externalProjectId)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            EntraObjectId = Guid.NewGuid().ToString(),
            Email = "owner@softim.local",
            DisplayName = "Owner",
            GlobalRole = GlobalRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var template = new ProjectTemplate
        {
            Id = Guid.NewGuid(),
            Name = $"Template-{Guid.NewGuid():N}",
            Description = "Test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var state = new TaskState
        {
            Id = Guid.NewGuid(),
            Name = "Todo",
            Color = "#0000ff",
            SortOrder = 1,
            IsActive = true,
            IsDefault = true,
            IsClosedState = false,
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
            Name = "Acme",
            Code = "ACME",
            Status = ProjectStatus.Active,
            ProjectTemplateId = template.Id,
            ExternalSystem = "Email",
            ExternalProjectId = externalProjectId,
            NextTicketNumber = 1,
            CreatedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        db.ProjectTemplates.Add(template);
        db.TaskStates.Add(state);
        db.TicketPriorities.Add(priority);
        db.Projects.Add(project);
        db.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = user.Id,
            JoinedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        return (project, user.Id, state.Id, priority.Id);
    }
}
