using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace SoftimProject.WebApi.Tests.Integration;

// End-to-end check that the authorization pipeline behaviors (IRequireProjectAccess and
// IRequireProjectRole) translate a forbidden call into HTTP 403. Tests are grouped by marker
// interface; the role-matrix cases exercise the hierarchy Admin > ProjectManager > Developer > Guest.
[Collection("Integration")]
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

    // --- Role-matrix cases — AuthorizationBehavior.IRequireProjectRole ---
    // These assert that the *authorization* check fires (403) regardless of whether the
    // target entity exists. The check runs before the handler body, so using a real board
    // id vs. a random guid would still produce the same outcome for the forbidden cases.

    [Fact]
    public async Task Developer_cannot_update_board_on_own_project_pm_required()
    {
        using var client = ClientAs(TestDataSeeder.UserAOid);
        var payload = new
        {
            projectId = TestDataSeeder.ProjectAId,
            boardId = TestDataSeeder.ProjectABoardId,
            name = "Renamed",
        };

        var response = await client.PutAsJsonAsync(
            $"/api/v1/projects/{TestDataSeeder.ProjectAId}/boards/{TestDataSeeder.ProjectABoardId}",
            payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ProjectManager_can_update_board_on_own_project()
    {
        using var client = ClientAs(TestDataSeeder.PmAOid);
        var payload = new
        {
            projectId = TestDataSeeder.ProjectAId,
            boardId = TestDataSeeder.ProjectABoardId,
            name = "Renamed by PM",
        };

        var response = await client.PutAsJsonAsync(
            $"/api/v1/projects/{TestDataSeeder.ProjectAId}/boards/{TestDataSeeder.ProjectABoardId}",
            payload);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Guest_cannot_update_ticket_developer_required()
    {
        using var client = ClientAs(TestDataSeeder.GuestAOid);
        var payload = new
        {
            projectId = TestDataSeeder.ProjectAId,
            ticketId = Guid.NewGuid(),
            title = "t",
            description = (string?)null,
            ticketPriorityId = Guid.NewGuid(),
            taskStateId = Guid.NewGuid(),
            assigneeId = (Guid?)null,
            dueDate = (DateOnly?)null,
            estimatedHours = (decimal?)null,
        };

        var response = await client.PutAsJsonAsync(
            $"/api/v1/projects/{TestDataSeeder.ProjectAId}/tickets/{payload.ticketId}",
            payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Guest_cannot_create_worklog_developer_required()
    {
        using var client = ClientAs(TestDataSeeder.GuestAOid);
        var payload = new
        {
            projectId = TestDataSeeder.ProjectAId,
            ticketId = Guid.NewGuid(),
            date = DateOnly.FromDateTime(DateTime.UtcNow),
            hours = 1.0m,
            description = "Guest is not allowed to log work here.",
            isBillable = false,
        };

        var response = await client.PostAsJsonAsync("/api/v1/worklogs", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Developer_cannot_delete_ticket_pm_required()
    {
        using var client = ClientAs(TestDataSeeder.UserAOid);

        var response = await client.DeleteAsync(
            $"/api/v1/projects/{TestDataSeeder.ProjectAId}/tickets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Non_admin_cannot_create_project()
    {
        using var client = ClientAs(TestDataSeeder.UserAOid);
        var payload = new { name = "New", code = "NEWP" };

        var response = await client.PostAsJsonAsync("/api/v1/projects", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
