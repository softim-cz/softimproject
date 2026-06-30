using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;
using SoftimProject.Infrastructure.Services;
using SoftimProject.Infrastructure.Services.Integrations;

namespace SoftimProject.Infrastructure.Tests;

public class MigrationCredentialResolverTests
{
    private static (MigrationCredentialResolver resolver, ApplicationDbContext db, DataProtectionSecretProtector protector) Build()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var protector = new DataProtectionSecretProtector(new EphemeralDataProtectionProvider());
        return (new MigrationCredentialResolver(db, protector), db, protector);
    }

    [Fact]
    public async Task PassesThrough_When_No_ConnectionId()
    {
        var (resolver, _, _) = Build();

        var (baseUrl, apiKey) = await resolver.ResolveAsync("https://ep.example", "typed-key", null, CancellationToken.None);

        baseUrl.Should().Be("https://ep.example");
        apiKey.Should().Be("typed-key");
    }

    [Fact]
    public async Task Uses_Stored_Url_And_Decrypted_Token_When_ConnectionId_Given()
    {
        var (resolver, db, protector) = Build();
        var id = Guid.NewGuid();
        db.IntegrationConnections.Add(new IntegrationConnection
        {
            Id = id,
            Name = "EP",
            SourceSystem = SyncType.EasyProject,
            BaseUrl = "https://stored.example",
            EncryptedApiToken = protector.Protect("stored-secret"),
        });
        await db.SaveChangesAsync();

        // Even if the caller passes other values, the saved connection wins.
        var (baseUrl, apiKey) = await resolver.ResolveAsync("https://typed.example", "typed", id, CancellationToken.None);

        baseUrl.Should().Be("https://stored.example");
        apiKey.Should().Be("stored-secret");
    }
}
