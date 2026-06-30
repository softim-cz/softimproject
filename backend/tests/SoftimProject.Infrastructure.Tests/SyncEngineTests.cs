using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SoftimProject.Application.Features.Migration.EasyProject.Dtos;
using SoftimProject.Application.Integrations;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;
using SoftimProject.Infrastructure.Services.EasyProject;
using SoftimProject.Infrastructure.Services.Integrations;

namespace SoftimProject.Infrastructure.Tests;

// Characterization tests for the extracted, provider-agnostic SyncEngine. They lock the
// behavior the EasyProject one-time migration relied on (external-system string, comment
// source, synthetic user keys, idempotent upserts) so future refactors can't drift.
public class SyncEngineTests
{
    private static readonly Guid TemplateId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid MappedUserId = Guid.NewGuid();

    [Fact]
    public async Task Migrates_Project_Ticket_Comment_Worklog_Checklist_CustomField()
    {
        await using var db = CreateDbContext();
        var (doneStateId, _) = await SeedAsync(db);
        var jobId = await SeedJobAsync(db);

        var connector = BuildConnector();
        var engine = BuildEngine(db, out var tracker);
        tracker.Init(jobId);

        await engine.ExecuteAsync(jobId, AdminId, BuildRequest(doneStateId), connector, new SourceConnectionContext("https://ep.example", "key"), JobSink(db, jobId));

        var project = await db.Projects.SingleAsync();
        project.ExternalSystem.Should().Be("EasyProject");
        project.ExternalProjectId.Should().Be("50");
        project.Name.Should().Be("Web Project");
        project.ProjectTemplateId.Should().Be(TemplateId);
        project.Description.Should().Contain("Body");

        var ticket = await db.Tickets.SingleAsync();
        ticket.ExternalId.Should().Be("100");
        ticket.Title.Should().Be("Login broken");
        ticket.TaskStateId.Should().Be(doneStateId);
        ticket.AssigneeId.Should().Be(MappedUserId);
        ticket.ReporterId.Should().Be(AdminId); // author "8" not mapped -> admin fallback
        ticket.ExternalUrl.Should().Be("https://ep.example/issues/100");
        ticket.ExternalProject.Should().Be("Web Project");
        ticket.Description.Should().Contain("HTML body");

        var comment = await db.Comments.SingleAsync();
        comment.Source.Should().Be(CommentSource.EasyProject);
        comment.ExternalId.Should().Be("1");
        comment.IsInternal.Should().BeTrue();
        comment.Content.Should().Contain("Real comment");

        var worklog = await db.Worklogs.SingleAsync();
        worklog.Source.Should().Be(WorklogSource.Sync);
        worklog.ExternalId.Should().Be("200");
        worklog.Hours.Should().Be(2.5m);
        worklog.IsBillable.Should().BeTrue();

        var checklist = await db.ChecklistItems.SingleAsync();
        checklist.ExternalId.Should().Be("31");
        checklist.IsCompleted.Should().BeTrue();

        var def = await db.CustomFieldDefinitions.SingleAsync();
        def.Name.Should().Be("Severity");
        def.FieldType.Should().Be(CustomFieldType.Select);
        var value = await db.TicketCustomFieldValues.SingleAsync();
        value.Value.Should().Be("High");

        var job = await db.MigrationJobs.SingleAsync();
        job.Status.Should().Be(MigrationStatus.Completed);
        job.CurrentPhase.Should().Be(MigrationPhase.Done);
        job.ProjectsMigrated.Should().Be(1);
        job.TicketsMigrated.Should().Be(1);
        job.ItemsFailed.Should().Be(0);
    }

    [Fact]
    public async Task Second_Run_Is_Idempotent_NoDuplicates()
    {
        await using var db = CreateDbContext();
        var (doneStateId, _) = await SeedAsync(db);

        var connector = BuildConnector();

        // First run
        var job1 = await SeedJobAsync(db);
        var engine1 = BuildEngine(db, out var tracker1);
        tracker1.Init(job1);
        await engine1.ExecuteAsync(job1, AdminId, BuildRequest(doneStateId), connector, new SourceConnectionContext("https://ep.example", "key"), JobSink(db, job1));

        // Second run with the same source data
        var job2 = await SeedJobAsync(db);
        var engine2 = BuildEngine(db, out var tracker2);
        tracker2.Init(job2);
        await engine2.ExecuteAsync(job2, AdminId, BuildRequest(doneStateId), connector, new SourceConnectionContext("https://ep.example", "key"), JobSink(db, job2));

        (await db.Projects.CountAsync()).Should().Be(1);
        (await db.Tickets.CountAsync()).Should().Be(1);
        (await db.Comments.CountAsync()).Should().Be(1);
        (await db.Worklogs.CountAsync()).Should().Be(1);
        (await db.ChecklistItems.CountAsync()).Should().Be(1);
        (await db.CustomFieldDefinitions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Incremental_Run_Passes_ChangedSince_To_Connector()
    {
        await using var db = CreateDbContext();
        var (doneStateId, _) = await SeedAsync(db);
        var jobId = await SeedJobAsync(db);

        var connector = BuildConnector();
        var engine = BuildEngine(db, out var tracker);
        tracker.Init(jobId);

        var since = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var request = BuildRequest(doneStateId) with { ChangedSince = since };

        await engine.ExecuteAsync(jobId, AdminId, request, connector, new SourceConnectionContext("https://ep.example", "key"), NullSyncJobSink.Instance);

        connector.LastIssuesChangedSince.Should().Be(since);
    }

    [Fact]
    public async Task Links_Created_Project_To_Connection()
    {
        await using var db = CreateDbContext();
        var (doneStateId, _) = await SeedAsync(db);
        var jobId = await SeedJobAsync(db);

        var connectionId = Guid.NewGuid();
        var connector = BuildConnector();
        var engine = BuildEngine(db, out var tracker);
        tracker.Init(jobId);

        var request = BuildRequest(doneStateId) with { IntegrationConnectionId = connectionId };
        await engine.ExecuteAsync(jobId, AdminId, request, connector, new SourceConnectionContext("https://ep.example", "key"), NullSyncJobSink.Instance);

        var project = await db.Projects.SingleAsync();
        project.IntegrationConnectionId.Should().Be(connectionId);
    }

    [Fact]
    public async Task SourceOwnedWins_Skips_Unchanged_Source_But_Applies_Changes()
    {
        await using var db = CreateDbContext();
        var (doneStateId, _) = await SeedAsync(db);
        var t1 = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);

        // Run 1: initial import (source @ t1).
        await RunOnce(db, doneStateId, IssueAt(t1, "Original"));
        (await db.Tickets.SingleAsync()).Title.Should().Be("Original");

        // A user edits the ticket in ProjectMan.
        var ticket = await db.Tickets.SingleAsync();
        ticket.Title = "Locally edited";
        await db.SaveChangesAsync();

        // Run 2: source unchanged (same t1) → local edit preserved.
        await RunOnce(db, doneStateId, IssueAt(t1, "Original"));
        (await db.Tickets.SingleAsync()).Title.Should().Be("Locally edited");

        // Run 3: source genuinely changed (t1+1h) → source wins.
        await RunOnce(db, doneStateId, IssueAt(t1.AddHours(1), "Updated from source"));
        (await db.Tickets.SingleAsync()).Title.Should().Be("Updated from source");
    }

    [Fact]
    public async Task Assigns_Distinct_Sequential_Ticket_Numbers()
    {
        await using var db = CreateDbContext();
        var (doneStateId, _) = await SeedAsync(db);
        var jobId = await SeedJobAsync(db);

        // Two issues in one project: each ticket must get its own per-project Number
        // (unique index ProjectId+Number). The bug left Number at 0 for all → second insert
        // collided and cascaded into every later ticket.
        var connector = new FakeSourceConnector
        {
            Projects = [new CanonicalProject("50", "Web Project", null, CanonicalProjectStatus.Active, null, null, null, [])],
            Issues = [Issue("100", "First"), Issue("101", "Second")],
        };
        var engine = BuildEngine(db, out var tracker);
        tracker.Init(jobId);

        await engine.ExecuteAsync(jobId, AdminId, BuildRequest(doneStateId), connector,
            new SourceConnectionContext("https://ep.example", "key"), JobSink(db, jobId));

        var numbers = await db.Tickets.Select(t => t.Number).ToListAsync();
        numbers.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        numbers.Should().NotContain(0); // bug assigned 0 to every ticket
        (await db.Projects.SingleAsync()).NextTicketNumber.Should().Be(3); // started at 1, two tickets consumed

        var job = await db.MigrationJobs.SingleAsync();
        job.TicketsMigrated.Should().Be(2);
        job.ItemsFailed.Should().Be(0);
    }

    [Fact]
    public async Task Worklog_With_Overlong_Note_Is_Truncated_To_Column_Limit()
    {
        await using var db = CreateDbContext();
        var (doneStateId, _) = await SeedAsync(db);
        var jobId = await SeedJobAsync(db);

        // Worklog.Description is nvarchar(2000); a long source note must be trimmed so the insert
        // doesn't abort with a truncation error (which previously failed the whole worklog batch).
        var longNote = "<p>" + new string('x', 5000) + "</p>";
        var connector = new FakeSourceConnector
        {
            Projects = [new CanonicalProject("50", "Web Project", null, CanonicalProjectStatus.Active, null, null, null, [])],
            Issues = [Issue("100", "First")],
            Worklogs = [new CanonicalWorklog("200", "100", new CanonicalUserRef("7", "Jane"), "2026-01-05", 2.5m, longNote, IsBillable: true)],
        };
        var engine = BuildEngine(db, out var tracker);
        tracker.Init(jobId);

        await engine.ExecuteAsync(jobId, AdminId, BuildRequest(doneStateId), connector,
            new SourceConnectionContext("https://ep.example", "key"), JobSink(db, jobId));

        var worklog = await db.Worklogs.SingleAsync();
        worklog.Description.Length.Should().BeLessThanOrEqualTo(2000);

        var job = await db.MigrationJobs.SingleAsync();
        job.Status.Should().Be(MigrationStatus.Completed);
        job.ItemsFailed.Should().Be(0);
    }

    [Fact]
    public async Task Duplicate_CustomField_On_Same_Ticket_Collapses_To_One_Value()
    {
        await using var db = CreateDbContext();
        var (doneStateId, _) = await SeedAsync(db);
        var jobId = await SeedJobAsync(db);

        // Two source values with the same field name resolve to one definition; without pending-aware
        // dedup both would insert and violate the unique (TicketId, DefinitionId) index. Last wins.
        var issue = new CanonicalIssue(
            "100", "Dup fields", null, null, "2", "Done", null, null, null, null, null, null, "50", "Web Project",
            [
                new CanonicalCustomFieldValue("9", "Severity", "High", CanonicalFieldFormat.Select, ["Low", "High"]),
                new CanonicalCustomFieldValue("10", "Severity", "Low", CanonicalFieldFormat.Select, ["Low", "High"]),
            ],
            [], [], [])
        { WebUrl = "https://ep.example/issues/100" };

        var connector = new FakeSourceConnector
        {
            Projects = [new CanonicalProject("50", "Web Project", null, CanonicalProjectStatus.Active, null, null, null, [])],
            Issues = [issue],
        };
        var engine = BuildEngine(db, out var tracker);
        tracker.Init(jobId);

        await engine.ExecuteAsync(jobId, AdminId, BuildRequest(doneStateId), connector,
            new SourceConnectionContext("https://ep.example", "key"), JobSink(db, jobId));

        (await db.CustomFieldDefinitions.CountAsync()).Should().Be(1);
        var values = await db.TicketCustomFieldValues.ToListAsync();
        values.Should().ContainSingle();
        values[0].Value.Should().Be("Low");

        var job = await db.MigrationJobs.SingleAsync();
        job.Status.Should().Be(MigrationStatus.Completed);
        job.ItemsFailed.Should().Be(0);
    }

    private static CanonicalIssue Issue(string externalId, string title) => new(
        externalId, title, null, null, "2", "Done", null, null, null, null, null, null, "50", "Web Project",
        [], [], [], [])
    { WebUrl = $"https://ep.example/issues/{externalId}" };

    private static CanonicalIssue IssueAt(DateTime sourceUpdatedAt, string title) => new(
        "100", title, null, null, "2", "Done", null, null, null, null, null, null, "50", "Web Project",
        [], [], [], [])
    { WebUrl = "https://ep.example/issues/100", SourceUpdatedAt = sourceUpdatedAt };

    private static async Task RunOnce(ApplicationDbContext db, Guid doneStateId, CanonicalIssue issue)
    {
        var jobId = await SeedJobAsync(db);
        var engine = BuildEngine(db, out var tracker);
        tracker.Init(jobId);
        var connector = new FakeSourceConnector
        {
            Projects = [new CanonicalProject("50", "Web Project", null, CanonicalProjectStatus.Active, null, null, null, [])],
            Issues = [issue],
        };
        await engine.ExecuteAsync(jobId, AdminId, BuildRequest(doneStateId), connector,
            new SourceConnectionContext("https://ep.example", "key"), NullSyncJobSink.Instance);
    }

    private static SyncEngineRequest BuildRequest(Guid doneStateId) => new(
        TemplateId,
        ["50"],
        new Dictionary<string, Guid?>(),
        new Dictionary<string, Guid> { ["2"] = doneStateId },
        new Dictionary<string, Guid>(),
        new Dictionary<string, Guid?> { ["7"] = MappedUserId, ["8"] = null },
        SkipClosedIssues: false,
        SkipAttachments: true,
        ImportComments: true,
        ImportWorklogs: true,
        ImportChecklists: true,
        CreateMissingUsers: false,
        AutoCreateTrackers: null,
        AutoCreateStatuses: null,
        AutoCreateStatusIsClosed: null,
        AutoCreatePriorities: null);

    private static FakeSourceConnector BuildConnector()
    {
        var issue = new CanonicalIssue(
            "100", "Login broken", "<p>HTML body</p>",
            TypeExternalId: null,
            StatusExternalId: "2", StatusName: "Done",
            PriorityExternalId: null,
            Assignee: new CanonicalUserRef("7", "Jane"),
            Reporter: new CanonicalUserRef("8", "John"),
            EstimatedHours: 3m, DueDate: "2026-02-01",
            ParentExternalId: null, ProjectExternalId: "50", ProjectName: "Web Project",
            CustomFields: [new CanonicalCustomFieldValue("9", "Severity", "High", CanonicalFieldFormat.Select, ["Low", "High"])],
            Comments: [new CanonicalComment("1", new CanonicalUserRef("8", "John"), "Real comment", IsInternal: true, CreatedAt: DateTime.UtcNow)],
            Attachments: [],
            ChecklistItems: [new CanonicalChecklistItem("31", "First", 0, IsCompleted: true)])
        {
            WebUrl = "https://ep.example/issues/100"
        };

        return new FakeSourceConnector
        {
            Projects = [new CanonicalProject("50", "Web Project", "<p>Body</p>", CanonicalProjectStatus.Active, null, null, null, [])],
            Issues = [issue],
            Worklogs = [new CanonicalWorklog("200", "100", new CanonicalUserRef("7", "Jane"), "2026-01-05", 2.5m, "work done", IsBillable: true)],
        };
    }

    private static SyncEngine BuildEngine(ApplicationDbContext db, out MigrationProgressTracker tracker)
    {
        tracker = new MigrationProgressTracker();
        return new SyncEngine(db, tracker, new FakeBlobStorage(), NullLogger<SyncEngine>.Instance);
    }

    // Wizard-style sink so tests can assert MigrationJob state.
    private static MigrationJobSink JobSink(ApplicationDbContext db, Guid jobId) => new(db, new NoopNotifier(), jobId);

    private static async Task<(Guid doneStateId, Guid priorityId)> SeedAsync(ApplicationDbContext db)
    {
        db.ProjectTemplates.Add(new ProjectTemplate { Id = TemplateId, Name = "Default", IsActive = true });
        var newState = new TaskState { Id = Guid.NewGuid(), Name = "New", Color = "#fff", SortOrder = 0, IsActive = true, IsDefault = true, ProjectTemplateId = TemplateId };
        var doneState = new TaskState { Id = Guid.NewGuid(), Name = "Done", Color = "#0f0", SortOrder = 1, IsActive = true, IsDefault = false, IsClosedState = true, ProjectTemplateId = TemplateId };
        db.TaskStates.AddRange(newState, doneState);
        var priority = new TicketPriority { Id = Guid.NewGuid(), Name = "Normal", Color = "#888", SortOrder = 0, IsActive = true, IsDefault = true, ProjectTemplateId = TemplateId };
        db.TicketPriorities.Add(priority);
        db.Users.Add(new User { Id = AdminId, EntraObjectId = "admin-oid", Email = "admin@x.cz", DisplayName = "Admin", GlobalRole = GlobalRole.Admin, IsActive = true });
        db.Users.Add(new User { Id = MappedUserId, EntraObjectId = "jane-oid", Email = "jane@x.cz", DisplayName = "Jane", GlobalRole = GlobalRole.User, IsActive = true });
        await db.SaveChangesAsync();
        return (doneState.Id, priority.Id);
    }

    private static async Task<Guid> SeedJobAsync(ApplicationDbContext db)
    {
        var jobId = Guid.NewGuid();
        db.MigrationJobs.Add(new MigrationJob
        {
            Id = jobId,
            InitiatedByUserId = AdminId,
            SourceSystem = "EasyProject",
            SourceBaseUrl = "https://ep.example",
            Status = MigrationStatus.Pending,
            CurrentPhase = MigrationPhase.Pending,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return jobId;
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class FakeSourceConnector : ISourceConnector
    {
        public IReadOnlyList<CanonicalProject> Projects { get; init; } = [];
        public IReadOnlyList<CanonicalIssue> Issues { get; init; } = [];
        public IReadOnlyList<CanonicalWorklog> Worklogs { get; init; } = [];

        // Records the changedSince the engine passed through (for incremental assertions).
        public DateTime? LastIssuesChangedSince { get; private set; }

        public SyncType SourceSystem => SyncType.EasyProject;
        public Task<(bool Success, string? Error)> TestConnectionAsync(SourceConnectionContext context, CancellationToken ct) => Task.FromResult((true, (string?)null));
        public Task<IReadOnlyList<CanonicalProject>> GetProjectsAsync(SourceConnectionContext context, CancellationToken ct) => Task.FromResult(Projects);
        public Task<IReadOnlyList<CanonicalUser>> GetUsersAsync(SourceConnectionContext context, CancellationToken ct) => Task.FromResult<IReadOnlyList<CanonicalUser>>([]);
        public Task<CanonicalLookups> GetLookupsAsync(SourceConnectionContext context, CancellationToken ct) => Task.FromResult(new CanonicalLookups([], [], []));
        public Task<IReadOnlyList<CanonicalIssue>> GetIssuesAsync(SourceConnectionContext context, string projectExternalId, DateTime? changedSince, CancellationToken ct) { LastIssuesChangedSince = changedSince; return Task.FromResult(Issues); }
        public Task<IReadOnlyList<CanonicalWorklog>> GetWorklogsAsync(SourceConnectionContext context, string projectExternalId, DateTime? changedSince, CancellationToken ct) => Task.FromResult(Worklogs);
        public Task<Stream> DownloadAttachmentAsync(SourceConnectionContext context, string contentUrl, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class NoopNotifier : IMigrationNotifier
    {
        public Task NotifyProgressAsync(Guid jobId, MigrationProgressDto progress) => Task.CompletedTask;
        public Task SendFetchProgressAsync(string sessionId, string message, int current, int total) => Task.CompletedTask;
        public Task SendIssueCountAsync(string sessionId, int epId, int issueCount) => Task.CompletedTask;
    }

    private sealed class FakeBlobStorage : IBlobStorageService
    {
        public Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
            => Task.FromResult($"https://blob/{containerName}/{blobName}");
        public Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
