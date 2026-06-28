using FluentAssertions;
using SoftimProject.Application.Integrations;
using SoftimProject.Application.Integrations.Jira;
using SoftimProject.Infrastructure.Services.Jira;

namespace SoftimProject.Infrastructure.Tests;

public class JiraCanonicalMapperTests
{
    [Theory]
    [InlineData(3600, 1.0)]
    [InlineData(5400, 1.5)]
    [InlineData(0, 0.0)]
    public void SecondsToHours_Converts(long seconds, double expected)
        => JiraCanonicalMapper.SecondsToHours(seconds).Should().Be((decimal)expected);

    [Fact]
    public void SecondsToHours_Null_StaysNull()
        => JiraCanonicalMapper.SecondsToHours(null).Should().BeNull();

    [Fact]
    public void IsClosedStatus_TrueOnlyForDoneCategory()
    {
        JiraCanonicalMapper.IsClosedStatus(new JiraStatus("1", "Done", new JiraStatusCategory("done"))).Should().BeTrue();
        JiraCanonicalMapper.IsClosedStatus(new JiraStatus("2", "In Progress", new JiraStatusCategory("indeterminate"))).Should().BeFalse();
        JiraCanonicalMapper.IsClosedStatus(new JiraStatus("3", "New", null)).Should().BeFalse();
    }

    [Fact]
    public void MapLookups_MapsTypesStatusesPriorities()
    {
        var lookups = JiraCanonicalMapper.MapLookups(
            [new JiraNamedEntity("10", "Bug")],
            [new JiraStatus("1", "Done", new JiraStatusCategory("done")), new JiraStatus("2", "Open", new JiraStatusCategory("new"))],
            [new JiraNamedEntity("3", "High")]);

        lookups.Types.Should().ContainSingle().Which.Should().BeEquivalentTo(new CanonicalLookup("10", "Bug", false));
        lookups.Statuses.Single(s => s.ExternalId == "1").IsClosed.Should().BeTrue();
        lookups.Statuses.Single(s => s.ExternalId == "2").IsClosed.Should().BeFalse();
        lookups.Priorities.Should().ContainSingle().Which.Name.Should().Be("High");
    }

    [Fact]
    public void MapProject_UsesNameThenKeyThenId()
    {
        JiraCanonicalMapper.MapProject(new JiraProject("100", "WEB", "Web Project", "desc"))
            .Should().BeEquivalentTo(new CanonicalProject("100", "Web Project", "desc", CanonicalProjectStatus.Active, null, null, null, []));

        JiraCanonicalMapper.MapProject(new JiraProject("101", "KEY", null, null)).Name.Should().Be("KEY");
        JiraCanonicalMapper.MapProject(new JiraProject("102", null, null, null)).Name.Should().Be("102");
    }

    [Fact]
    public void MapUserRef_NullWhenNoAccountId()
    {
        JiraCanonicalMapper.MapUserRef(null).Should().BeNull();
        JiraCanonicalMapper.MapUserRef(new JiraUser(null, "X", null)).Should().BeNull();
        JiraCanonicalMapper.MapUserRef(new JiraUser("acc-1", "Jane", null))
            .Should().BeEquivalentTo(new CanonicalUserRef("acc-1", "Jane"));
    }

    [Fact]
    public void MapIssue_MapsCoreFields_DescriptionFromRendered_WebUrlFromKey()
    {
        var issue = new JiraIssue(
            Id: "10001",
            Key: "WEB-42",
            Fields: new JiraFields(
                Summary: "Login broken",
                IssueType: new JiraRef("10", "Bug"),
                Status: new JiraStatus("3", "In Progress", new JiraStatusCategory("indeterminate")),
                Priority: new JiraRef("2", "High"),
                Assignee: new JiraUser("acc-jane", "Jane", "jane@x.cz"),
                Reporter: new JiraUser("acc-john", "John", null),
                TimeOriginalEstimate: 7200,
                DueDate: "2026-02-01",
                Parent: new JiraParent("9000", "WEB-1"),
                Project: new JiraProjectRef("100", "WEB", "Web Project"),
                Updated: "2026-06-27T10:30:00.000+0000"),
            RenderedFields: new JiraRenderedFields("<p>HTML body</p>"));

        var mapped = JiraCanonicalMapper.MapIssue(issue, "https://acme.atlassian.net/");

        mapped.ExternalId.Should().Be("10001");
        mapped.Title.Should().Be("Login broken");
        mapped.DescriptionHtml.Should().Be("<p>HTML body</p>");
        mapped.TypeExternalId.Should().Be("10");
        mapped.StatusExternalId.Should().Be("3");
        mapped.StatusName.Should().Be("In Progress");
        mapped.PriorityExternalId.Should().Be("2");
        mapped.Assignee!.ExternalId.Should().Be("acc-jane");
        mapped.Reporter!.ExternalId.Should().Be("acc-john");
        mapped.EstimatedHours.Should().Be(2.0m);
        mapped.DueDate.Should().Be("2026-02-01");
        mapped.ParentExternalId.Should().Be("9000");
        mapped.ProjectExternalId.Should().Be("100");
        mapped.ProjectName.Should().Be("Web Project");
        mapped.WebUrl.Should().Be("https://acme.atlassian.net/browse/WEB-42");
        mapped.SourceUpdatedAt.Should().NotBeNull();
        // Rich content out of scope for the initial connector.
        mapped.Comments.Should().BeEmpty();
        mapped.Attachments.Should().BeEmpty();
    }

    [Fact]
    public void MapIssue_HandlesMissingFields()
    {
        var issue = new JiraIssue("1", "WEB-1", Fields: null, RenderedFields: null);
        var mapped = JiraCanonicalMapper.MapIssue(issue, "https://x");
        mapped.Title.Should().BeEmpty();
        mapped.StatusExternalId.Should().BeNull();
        mapped.Assignee.Should().BeNull();
        mapped.EstimatedHours.Should().BeNull();
        mapped.WebUrl.Should().Be("https://x/browse/WEB-1");
    }
}
