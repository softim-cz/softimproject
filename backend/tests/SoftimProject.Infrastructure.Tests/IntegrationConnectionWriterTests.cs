using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Features.Migration.EasyProject;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;
using SoftimProject.Infrastructure.Services;
using SoftimProject.Infrastructure.Services.Integrations;

namespace SoftimProject.Infrastructure.Tests;

public class IntegrationConnectionWriterTests
{
    private static readonly Guid CreatorId = Guid.NewGuid();

    private static StartMigrationCommand Command(string token = "secret-token", bool enableIncremental = false, int interval = 1440) => new(
        BaseUrl: "https://ep.example",
        ApiKey: token,
        ProjectIds: [1, 2],
        TargetProjectTemplateId: Guid.NewGuid(),
        TrackerMapping: new Dictionary<int, Guid?>(),
        StatusMapping: new Dictionary<int, Guid> { [2] = Guid.NewGuid() },
        PriorityMapping: new Dictionary<int, Guid>(),
        UserMapping: new Dictionary<int, Guid?>(),
        SkipClosedIssues: false,
        SkipAttachments: true,
        ImportComments: true,
        ImportWorklogs: true,
        ImportChecklists: false,
        CreateMissingUsers: false,
        EnableIncrementalSync: enableIncremental,
        SyncIntervalMinutes: interval);

    private static (IntegrationConnectionWriter writer, ApplicationDbContext db, DataProtectionSecretProtector protector) Build()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var protector = new DataProtectionSecretProtector(new EphemeralDataProtectionProvider());
        return (new IntegrationConnectionWriter(db, protector), db, protector);
    }

    [Fact]
    public async Task Creates_Connection_With_Encrypted_Token_And_Manual_Defaults()
    {
        var (writer, db, protector) = Build();
        var cmd = Command();

        var id = await writer.UpsertForEasyProjectAsync(cmd, CreatorId, CancellationToken.None);

        var connection = await db.IntegrationConnections.SingleAsync();
        connection.Id.Should().Be(id);
        connection.CreatedByUserId.Should().Be(CreatorId);
        connection.SourceSystem.Should().Be(SyncType.EasyProject);
        connection.BaseUrl.Should().Be("https://ep.example");
        connection.Name.Should().Contain("ep.example");

        connection.EncryptedApiToken.Should().NotBeNullOrEmpty();
        connection.EncryptedApiToken.Should().NotBe("secret-token");
        protector.Unprotect(connection.EncryptedApiToken).Should().Be("secret-token");

        connection.Mode.Should().Be(IntegrationSyncMode.Manual);
        connection.IsEnabled.Should().BeFalse();
        connection.IntervalMinutes.Should().Be(1440);
        connection.ProjectSelectorJson.Should().Contain("1").And.Contain("2");
        connection.MappingsJson.Should().NotBeNullOrEmpty();
        connection.OptionsJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Upsert_Applies_Wizard_Scheduling()
    {
        var (writer, db, _) = Build();

        // First run enables incremental sync at 1h.
        var firstId = await writer.UpsertForEasyProjectAsync(
            Command("token-1", enableIncremental: true, interval: 60), CreatorId, CancellationToken.None);

        var created = await db.IntegrationConnections.SingleAsync();
        created.Mode.Should().Be(IntegrationSyncMode.FullThenIncremental);
        created.IsEnabled.Should().BeTrue();
        created.IntervalMinutes.Should().Be(60);

        // Re-running the wizard with incremental off is the user's explicit intent and wins.
        var secondId = await writer.UpsertForEasyProjectAsync(
            Command("token-2", enableIncremental: false), CreatorId, CancellationToken.None);

        secondId.Should().Be(firstId); // same connection (upsert by system + baseUrl)
        (await db.IntegrationConnections.CountAsync()).Should().Be(1);

        var updated = await db.IntegrationConnections.SingleAsync();
        updated.EncryptedApiToken.Should().NotBeNull(); // credentials refreshed
        updated.Mode.Should().Be(IntegrationSyncMode.Manual);
        updated.IsEnabled.Should().BeFalse();
        updated.IntervalMinutes.Should().Be(1440);
    }
}
