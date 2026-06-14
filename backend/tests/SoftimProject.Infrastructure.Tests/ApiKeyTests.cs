using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SoftimProject.Application.Common;
using SoftimProject.Application.Features.ApiKeys;
using SoftimProject.Application.Interfaces;
using SoftimProject.Domain.Entities;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.Infrastructure.Tests;

public class ApiKeyTests
{
    [Fact]
    public void Generated_keys_are_unique_and_prefixed()
    {
        var a = ApiKeyHasher.Generate();
        var b = ApiKeyHasher.Generate();
        a.Should().StartWith("spk_");
        a.Should().NotBe(b);
        ApiKeyHasher.Hash(a).Should().Be(ApiKeyHasher.Hash(a)); // deterministic
        ApiKeyHasher.Hash(a).Should().NotBe(ApiKeyHasher.Hash(b));
    }

    [Fact]
    public async Task Generate_stores_only_the_hash_and_returns_plaintext_once()
    {
        await using var db = CreateDbContext();
        var user = await SeedUserAsync(db);
        var handler = new GenerateApiKeyCommandHandler(db, MockUser(user.Id).Object);

        var result = await handler.Handle(new GenerateApiKeyCommand("Postman", null), CancellationToken.None);

        result.PlaintextKey.Should().StartWith("spk_");
        result.Prefix.Should().NotContain(result.PlaintextKey[10..]); // only a short display prefix

        var stored = await db.ApiKeys.SingleAsync(k => k.Id == result.Id);
        stored.KeyHash.Should().Be(ApiKeyHasher.Hash(result.PlaintextKey));
        stored.KeyHash.Should().NotContain(result.PlaintextKey); // plaintext never stored
        stored.UserId.Should().Be(user.Id);
        stored.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Expiry_is_set_from_days()
    {
        await using var db = CreateDbContext();
        var user = await SeedUserAsync(db);
        var handler = new GenerateApiKeyCommandHandler(db, MockUser(user.Id).Object);

        var result = await handler.Handle(new GenerateApiKeyCommand("CI", 30), CancellationToken.None);

        result.ExpiresAt.Should().NotBeNull();
        result.ExpiresAt!.Value.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task List_returns_only_own_keys()
    {
        await using var db = CreateDbContext();
        var me = await SeedUserAsync(db);
        var other = await SeedUserAsync(db);
        await new GenerateApiKeyCommandHandler(db, MockUser(me.Id).Object)
            .Handle(new GenerateApiKeyCommand("mine", null), CancellationToken.None);
        await new GenerateApiKeyCommandHandler(db, MockUser(other.Id).Object)
            .Handle(new GenerateApiKeyCommand("theirs", null), CancellationToken.None);

        var mine = await new GetApiKeysQueryHandler(db, MockUser(me.Id).Object)
            .Handle(new GetApiKeysQuery(), CancellationToken.None);

        mine.Should().ContainSingle().Which.Name.Should().Be("mine");
    }

    [Fact]
    public async Task Revoke_sets_revoked_and_rejects_other_users_key()
    {
        await using var db = CreateDbContext();
        var owner = await SeedUserAsync(db);
        var intruder = await SeedUserAsync(db);
        var created = await new GenerateApiKeyCommandHandler(db, MockUser(owner.Id).Object)
            .Handle(new GenerateApiKeyCommand("k", null), CancellationToken.None);

        var intruderAttempt = () => new RevokeApiKeyCommandHandler(db, MockUser(intruder.Id).Object)
            .Handle(new RevokeApiKeyCommand(created.Id), CancellationToken.None);
        await intruderAttempt.Should().ThrowAsync<UnauthorizedAccessException>();

        await new RevokeApiKeyCommandHandler(db, MockUser(owner.Id).Object)
            .Handle(new RevokeApiKeyCommand(created.Id), CancellationToken.None);
        var stored = await db.ApiKeys.SingleAsync(k => k.Id == created.Id);
        stored.RevokedAt.Should().NotBeNull();
    }

    private static ApplicationDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Mock<ICurrentUserService> MockUser(Guid userId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(x => x.UserId).Returns(userId);
        mock.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(false);
        return mock;
    }

    private static async Task<User> SeedUserAsync(ApplicationDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            EntraObjectId = Guid.NewGuid().ToString(),
            Email = $"{Guid.NewGuid():N}@softim.local",
            DisplayName = "Tester",
            GlobalRole = GlobalRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
