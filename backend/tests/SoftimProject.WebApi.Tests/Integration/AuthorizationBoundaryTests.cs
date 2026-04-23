using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;

namespace SoftimProject.WebApi.Tests.Integration;

// End-to-end check that the IRequireProjectAccess pipeline behavior (AuthorizationBehavior)
// translates a cross-project call into HTTP 403. One test per entry-point family keeps the
// matrix readable; the comprehensive endpoint sweep lives in #22.
public sealed class AuthorizationBoundaryTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public AuthorizationBoundaryTests(IntegrationTestFactory factory) => _factory = factory;

    private HttpClient ClientAs(string devUserId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User-Id", devUserId);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    [Fact]
    public async Task UserA_cannot_list_tickets_of_ProjectB()
    {
        using var client = ClientAs(TestDataSeeder.UserAOid);

        var response = await client.GetAsync($"/api/v1/projects/{TestDataSeeder.ProjectBId}/tickets");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserA_cannot_read_ProjectB_detail()
    {
        using var client = ClientAs(TestDataSeeder.UserAOid);

        var response = await client.GetAsync($"/api/v1/projects/{TestDataSeeder.ProjectBId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserB_cannot_list_ProjectA_comments()
    {
        using var client = ClientAs(TestDataSeeder.UserBOid);

        var response = await client.GetAsync($"/api/v1/projects/{TestDataSeeder.ProjectAId}/comments");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserA_can_access_own_ProjectA()
    {
        using var client = ClientAs(TestDataSeeder.UserAOid);

        var response = await client.GetAsync($"/api/v1/projects/{TestDataSeeder.ProjectAId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_bypasses_project_membership_check()
    {
        using var client = ClientAs(TestDataSeeder.AdminOid);

        var response = await client.GetAsync($"/api/v1/projects/{TestDataSeeder.ProjectBId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
