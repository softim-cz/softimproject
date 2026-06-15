using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SoftimProject.Application.Features.Projects.GitHub;
using SoftimProject.Domain.Entities;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.Infrastructure.Tests;

public class GitHubMetadataMapperTests
{
    [Theory]
    [InlineData("priority: High", "High")]
    [InlineData("type/bug", "bug")]
    [InlineData("  Spaced  ", "Spaced")]
    [InlineData("kind:feature", "feature")]
    [InlineData("plain", "plain")]
    public void Normalize_Strips_Prefix_And_Trims(string raw, string expected)
        => GitHubMetadataMapper.Normalize(raw).Should().Be(expected);

    [Fact]
    public async Task ResolveAssigneeId_Matches_By_GitHubLogin_CaseInsensitive()
    {
        await using var db = CreateDbContext();
        var user = new User { Id = Guid.NewGuid(), GitHubLogin = "octocat", Email = "o@x.cz", DisplayName = "Octo" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var hit = await GitHubMetadataMapper.ResolveAssigneeIdAsync(db, ["OCTOCAT"], CancellationToken.None);
        hit.Should().Be(user.Id);

        var miss = await GitHubMetadataMapper.ResolveAssigneeIdAsync(db, ["nobody"], CancellationToken.None);
        miss.Should().BeNull();

        var empty = await GitHubMetadataMapper.ResolveAssigneeIdAsync(db, [], CancellationToken.None);
        empty.Should().BeNull();
    }

    [Fact]
    public async Task ResolvePriorityId_Matches_Label_To_Name_Within_Template()
    {
        await using var db = CreateDbContext();
        var templateId = Guid.NewGuid();
        var otherTemplateId = Guid.NewGuid();
        var high = new TicketPriority { Id = Guid.NewGuid(), Name = "High", ProjectTemplateId = templateId, IsActive = true, SortOrder = 1 };
        // Same name but a different template — must NOT win when scoped.
        var highOther = new TicketPriority { Id = Guid.NewGuid(), Name = "High", ProjectTemplateId = otherTemplateId, IsActive = true, SortOrder = 1 };
        db.TicketPriorities.AddRange(high, highOther);
        await db.SaveChangesAsync();

        var hit = await GitHubMetadataMapper.ResolvePriorityIdAsync(db, templateId, ["priority: high"], CancellationToken.None);
        hit.Should().Be(high.Id);

        var miss = await GitHubMetadataMapper.ResolvePriorityIdAsync(db, templateId, ["whatever"], CancellationToken.None);
        miss.Should().BeNull();
    }

    [Fact]
    public async Task ResolveTaskTypeId_Matches_Label_To_Name_Or_Localized()
    {
        await using var db = CreateDbContext();
        var bug = new TaskType { Id = Guid.NewGuid(), Name = "Bug", NameCs = "Chyba", IsActive = true, SortOrder = 1 };
        db.TaskTypes.Add(bug);
        await db.SaveChangesAsync();

        (await GitHubMetadataMapper.ResolveTaskTypeIdAsync(db, ["bug"], CancellationToken.None)).Should().Be(bug.Id);
        (await GitHubMetadataMapper.ResolveTaskTypeIdAsync(db, ["type/Chyba"], CancellationToken.None)).Should().Be(bug.Id);
        (await GitHubMetadataMapper.ResolveTaskTypeIdAsync(db, ["enhancement"], CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task ResolveAssigneeLogin_Returns_Login_For_Linked_User()
    {
        await using var db = CreateDbContext();
        var user = new User { Id = Guid.NewGuid(), GitHubLogin = "octocat", Email = "o@x.cz", DisplayName = "Octo" };
        var noGh = new User { Id = Guid.NewGuid(), GitHubLogin = null, Email = "n@x.cz", DisplayName = "NoGh" };
        db.Users.AddRange(user, noGh);
        await db.SaveChangesAsync();

        (await GitHubMetadataMapper.ResolveAssigneeLoginAsync(db, user.Id, CancellationToken.None)).Should().Be("octocat");
        (await GitHubMetadataMapper.ResolveAssigneeLoginAsync(db, noGh.Id, CancellationToken.None)).Should().BeNull();
        (await GitHubMetadataMapper.ResolveAssigneeLoginAsync(db, null, CancellationToken.None)).Should().BeNull();
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
