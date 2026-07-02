using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SoftimProject.Application.Features.Migration.EasyProject;
using SoftimProject.Application.Integrations;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;
using SoftimProject.Infrastructure.Services;
using SoftimProject.Infrastructure.Services.EasyProject;
using SoftimProject.Infrastructure.Services.Integrations;

namespace SoftimProject.Infrastructure.Tests;

public class ExternalSyncRunnerTests
{
    private static readonly Guid TemplateId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();

    [Fact]
    public async Task Runs_Incremental_Sync_Creates_Project_And_Advances_Watermark()
    {
        await using var db = CreateDbContext();
        var doneStateId = await SeedAsync(db);
        var protector = new DataProtectionSecretProtector(new EphemeralDataProtectionProvider());
        var connection = await SeedEnabledConnectionAsync(db, protector, doneStateId);

        var connector = BuildConnector();
        var runner = BuildRunner(db, protector, connector);

        var outcome = await runner.RunAsync(connection, CancellationToken.None);

        outcome.HardFailed.Should().BeFalse();
        (await db.Projects.CountAsync()).Should().Be(1);
        (await db.Tickets.CountAsync()).Should().Be(1);

        var reloaded = await db.IntegrationConnections.SingleAsync();
        reloaded.LastSyncWatermark.Should().NotBeNull();
        // First run had a null watermark → full pull.
        connector.LastIssuesChangedSince.Should().BeNull();
    }

    [Fact]
    public async Task Missing_Token_HardFails_Without_Advancing_Watermark()
    {
        await using var db = CreateDbContext();
        var doneStateId = await SeedAsync(db);
        var protector = new DataProtectionSecretProtector(new EphemeralDataProtectionProvider());
        var connection = await SeedEnabledConnectionAsync(db, protector, doneStateId);
        connection.EncryptedApiToken = null;
        await db.SaveChangesAsync();

        var runner = BuildRunner(db, protector, BuildConnector());

        var outcome = await runner.RunAsync(connection, CancellationToken.None);

        outcome.HardFailed.Should().BeTrue();
        outcome.Error.Should().Contain("token");
        (await db.Projects.CountAsync()).Should().Be(0);
        (await db.IntegrationConnections.SingleAsync()).LastSyncWatermark.Should().BeNull();
    }

    private static ExternalSyncRunner BuildRunner(ApplicationDbContext db, ISecretProtector protector, ISourceConnector connector)
    {
        // Engine and runner must share the tracker instance (singleton in production DI),
        // otherwise the runner can't read the engine's progress.
        var tracker = new MigrationProgressTracker();
        var engine = new SyncEngine(db, tracker, new FakeBlobStorage(), NullLogger<SyncEngine>.Instance);
        return new ExternalSyncRunner(db, protector, engine, [connector], tracker, NullLogger<ExternalSyncRunner>.Instance);
    }

    private static async Task<IntegrationConnection> SeedEnabledConnectionAsync(ApplicationDbContext db, ISecretProtector protector, Guid doneStateId)
    {
        // Build the connection through the real writer (so JSON config round-trips), then enable it.
        var cmd = new StartMigrationCommand(
            BaseUrl: "https://ep.example",
            ApiKey: "tok",
            ProjectIds: [50],
            TargetProjectTemplateId: TemplateId,
            TrackerMapping: new Dictionary<int, Guid?>(),
            StatusMapping: new Dictionary<int, Guid> { [2] = doneStateId },
            PriorityMapping: new Dictionary<int, Guid>(),
            UserMapping: new Dictionary<int, Guid?>(),
            SkipClosedIssues: false,
            SkipAttachments: true,
            ImportComments: false,
            ImportWorklogs: false,
            ImportChecklists: false,
            CreateMissingUsers: false);

        var writer = new IntegrationConnectionWriter(db, protector);
        await writer.UpsertForEasyProjectAsync(cmd, AdminId, CancellationToken.None);

        var connection = await db.IntegrationConnections.SingleAsync();
        connection.Mode = IntegrationSyncMode.IncrementalOnly;
        connection.IsEnabled = true;
        await db.SaveChangesAsync();
        return connection;
    }

    private static FakeSourceConnector BuildConnector()
    {
        var issue = new CanonicalIssue(
            "100", "Login broken", "<p>body</p>",
            TypeExternalId: null, StatusExternalId: "2", StatusName: "Done", PriorityExternalId: null,
            Assignee: null, Reporter: null, EstimatedHours: null, DueDate: null,
            ParentExternalId: null, ProjectExternalId: "50", ProjectName: "Web Project",
            CustomFields: [], Comments: [], Attachments: [], ChecklistItems: [])
        { WebUrl = "https://ep.example/issues/100" };

        return new FakeSourceConnector
        {
            Projects = [new CanonicalProject("50", "Web Project", null, CanonicalProjectStatus.Active, null, null, null, [])],
            Issues = [issue],
        };
    }

    private static async Task<Guid> SeedAsync(ApplicationDbContext db)
    {
        db.ProjectTemplates.Add(new ProjectTemplate { Id = TemplateId, Name = "Default", IsActive = true });
        var newState = new TaskState { Id = Guid.NewGuid(), Name = "New", Color = "#fff", SortOrder = 0, IsActive = true, IsDefault = true, ProjectTemplateId = TemplateId };
        var doneState = new TaskState { Id = Guid.NewGuid(), Name = "Done", Color = "#0f0", SortOrder = 1, IsActive = true, IsClosedState = true, ProjectTemplateId = TemplateId };
        db.TaskStates.AddRange(newState, doneState);
        db.TicketPriorities.Add(new TicketPriority { Id = Guid.NewGuid(), Name = "Normal", Color = "#888", SortOrder = 0, IsActive = true, IsDefault = true, ProjectTemplateId = TemplateId });
        db.Users.Add(new User { Id = AdminId, EntraObjectId = "admin-oid", Email = "admin@x.cz", DisplayName = "Admin", GlobalRole = GlobalRole.Admin, IsActive = true });
        await db.SaveChangesAsync();
        return doneState.Id;
    }

    private static ApplicationDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class FakeSourceConnector : ISourceConnector
    {
        public IReadOnlyList<CanonicalProject> Projects { get; init; } = [];
        public IReadOnlyList<CanonicalIssue> Issues { get; init; } = [];
        public DateTime? LastIssuesChangedSince { get; private set; }

        public SyncType SourceSystem => SyncType.EasyProject;
        public Task<(bool Success, string? Error)> TestConnectionAsync(SourceConnectionContext context, CancellationToken ct) => Task.FromResult((true, (string?)null));
        public Task<IReadOnlyList<CanonicalProject>> GetProjectsAsync(SourceConnectionContext context, CancellationToken ct) => Task.FromResult(Projects);
        public Task<IReadOnlyList<CanonicalUser>> GetUsersAsync(SourceConnectionContext context, CancellationToken ct) => Task.FromResult<IReadOnlyList<CanonicalUser>>([]);
        public Task<CanonicalLookups> GetLookupsAsync(SourceConnectionContext context, CancellationToken ct) => Task.FromResult(new CanonicalLookups([], [], []));
        public Task<IReadOnlyList<CanonicalIssue>> GetIssuesAsync(SourceConnectionContext context, string projectExternalId, DateTime? changedSince, CancellationToken ct) { LastIssuesChangedSince = changedSince; return Task.FromResult(Issues); }
        public Task<IReadOnlyList<CanonicalWorklog>> GetWorklogsAsync(SourceConnectionContext context, string projectExternalId, DateTime? changedSince, CancellationToken ct) => Task.FromResult<IReadOnlyList<CanonicalWorklog>>([]);
        public Task<Stream> DownloadAttachmentAsync(SourceConnectionContext context, string contentUrl, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeBlobStorage : IBlobStorageService
    {
        public bool IsConfigured => true;
        public Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType, CancellationToken cancellationToken = default) => Task.FromResult("https://blob/x");
        public Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
