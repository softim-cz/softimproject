using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SoftimProject.Domain.Enums;
using SoftimProject.Infrastructure.Persistence;

namespace SoftimProject.WebApi.Tests.Integration;

// Exercises the admin endpoints added in #38: global-role change, account activation,
// plus the self/last-admin guardrails that prevent the system from being locked out.
[Collection("Integration")]
public sealed class AdminUsersTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public AdminUsersTests(IntegrationTestFactory factory) => _factory = factory;

    private HttpClient ClientAs(string devUserId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User-Id", devUserId);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private async Task ResetUserAsync(Guid userId, GlobalRole role, bool isActive)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FindAsync(userId);
        if (user is null) return;
        user.GlobalRole = role;
        user.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Non_admin_cannot_change_global_role()
    {
        using var client = ClientAs(TestDataSeeder.UserAOid);
        var body = new { userId = TestDataSeeder.UserBId, globalRole = GlobalRole.Admin };

        var response = await client.PutAsJsonAsync($"/api/v1/admin/users/{TestDataSeeder.UserBId}/global-role", body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_can_promote_user_to_admin()
    {
        await ResetUserAsync(TestDataSeeder.UserBId, GlobalRole.User, true);
        using var client = ClientAs(TestDataSeeder.AdminOid);
        var body = new { userId = TestDataSeeder.UserBId, globalRole = GlobalRole.Admin };

        var response = await client.PutAsJsonAsync($"/api/v1/admin/users/{TestDataSeeder.UserBId}/global-role", body);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ResetUserAsync(TestDataSeeder.UserBId, GlobalRole.User, true);
    }

    [Fact]
    public async Task Admin_cannot_demote_self()
    {
        using var client = ClientAs(TestDataSeeder.AdminOid);
        var body = new { userId = TestDataSeeder.AdminId, globalRole = GlobalRole.User };

        var response = await client.PutAsJsonAsync($"/api/v1/admin/users/{TestDataSeeder.AdminId}/global-role", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cannot_demote_last_admin()
    {
        // Seeder has exactly one Admin (AdminId). Promote UserB first, demote UserB — ok.
        // Then promoting UserB to User again should fail because only the seeded Admin stays —
        // oh, but seeded Admin stays through ResetUserAsync, so "last-admin" only fires if we
        // first remove the original Admin's admin role. Simulate by promoting UserB then
        // deactivating AdminId, then demoting UserB — last active admin.
        await ResetUserAsync(TestDataSeeder.UserBId, GlobalRole.Admin, true);
        await ResetUserAsync(TestDataSeeder.AdminId, GlobalRole.Admin, false);

        using var client = ClientAs(TestDataSeeder.UserBOid);
        var body = new { userId = TestDataSeeder.UserBId, globalRole = GlobalRole.User };

        var response = await client.PutAsJsonAsync($"/api/v1/admin/users/{TestDataSeeder.UserBId}/global-role", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await ResetUserAsync(TestDataSeeder.UserBId, GlobalRole.User, true);
        await ResetUserAsync(TestDataSeeder.AdminId, GlobalRole.Admin, true);
    }

    [Fact]
    public async Task Admin_can_deactivate_user()
    {
        await ResetUserAsync(TestDataSeeder.UserBId, GlobalRole.User, true);
        using var client = ClientAs(TestDataSeeder.AdminOid);
        var body = new { userId = TestDataSeeder.UserBId, isActive = false };

        var response = await client.PutAsJsonAsync($"/api/v1/admin/users/{TestDataSeeder.UserBId}/active", body);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await ResetUserAsync(TestDataSeeder.UserBId, GlobalRole.User, true);
    }

    [Fact]
    public async Task Admin_cannot_deactivate_self()
    {
        using var client = ClientAs(TestDataSeeder.AdminOid);
        var body = new { userId = TestDataSeeder.AdminId, isActive = false };

        var response = await client.PutAsJsonAsync($"/api/v1/admin/users/{TestDataSeeder.AdminId}/active", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
