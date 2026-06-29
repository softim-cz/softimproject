using System.Text.Json;
using FluentAssertions;
using SoftimProject.Application.Features.Migration.EasyProject.Models;
using SoftimProject.Application.Integrations;
using SoftimProject.Infrastructure.Services.EasyProject;

namespace SoftimProject.Infrastructure.Tests;

public class EasyProjectCanonicalMapperTests
{
    [Fact]
    public void PossibleValue_Value_Accepts_Number_Or_String()
    {
        // EasyProject mixes numeric ("value":82) and string ("value":"90") possible_values.
        var json = """
            [{"id":23,"name":"Owner","field_format":"user","possible_values":[
                {"value":82,"label":"me"},
                {"value":"90","label":"Acond"},
                {"value":null,"label":"none"}
            ]}]
            """;

        var defs = JsonSerializer.Deserialize<List<EpCustomFieldDefinition>>(json);

        defs.Should().ContainSingle();
        var pv = defs![0].PossibleValues!;
        pv[0].Value.Should().Be("82");
        pv[1].Value.Should().Be("90");
        pv[2].Value.Should().BeNull();
    }

    [Theory]
    [InlineData(5, CanonicalProjectStatus.Completed)]
    [InlineData(9, CanonicalProjectStatus.Archived)]
    [InlineData(1, CanonicalProjectStatus.Active)]
    [InlineData(0, CanonicalProjectStatus.Active)]
    public void MapProjectStatus_MapsKnownCodes(int epStatus, CanonicalProjectStatus expected)
        => EasyProjectCanonicalMapper.MapProjectStatus(epStatus).Should().Be(expected);

    [Theory]
    [InlineData("int", CanonicalFieldFormat.Number)]
    [InlineData("float", CanonicalFieldFormat.Number)]
    [InlineData("date", CanonicalFieldFormat.Date)]
    [InlineData("list", CanonicalFieldFormat.Select)]
    [InlineData("enumeration", CanonicalFieldFormat.Select)]
    [InlineData("string", CanonicalFieldFormat.Text)]
    [InlineData(null, CanonicalFieldFormat.Text)]
    public void MapFieldFormat_MatchesEngineRules(string? format, CanonicalFieldFormat expected)
        => EasyProjectCanonicalMapper.MapFieldFormat(format).Should().Be(expected);

    [Fact]
    public void MapUserRef_NullStaysNull_OtherwiseStringifiesId()
    {
        EasyProjectCanonicalMapper.MapUserRef(null).Should().BeNull();

        var mapped = EasyProjectCanonicalMapper.MapUserRef(new EpRef(42, "Jane Doe"));
        mapped.Should().NotBeNull();
        mapped!.ExternalId.Should().Be("42");
        mapped.DisplayName.Should().Be("Jane Doe");
    }

    [Fact]
    public void MapUser_CarriesAllFields()
    {
        var user = new EpUser(7, "jdoe", "Jane", "Doe", "jane@x.cz", 1, "https://a/avatar.png");

        var mapped = EasyProjectCanonicalMapper.MapUser(user);

        mapped.ExternalId.Should().Be("7");
        mapped.Login.Should().Be("jdoe");
        mapped.FirstName.Should().Be("Jane");
        mapped.LastName.Should().Be("Doe");
        mapped.Email.Should().Be("jane@x.cz");
        mapped.AvatarUrl.Should().Be("https://a/avatar.png");
    }

    [Fact]
    public void MapLookups_CarriesIsClosedOnlyForStatuses()
    {
        var lookups = EasyProjectCanonicalMapper.MapLookups(
            [new EpTracker(1, "Bug")],
            [new EpIssueStatus(2, "Done", IsClosed: true), new EpIssueStatus(3, "New", IsClosed: false)],
            [new EpIssuePriority(4, "High")]);

        lookups.Types.Should().ContainSingle().Which.Should().BeEquivalentTo(new CanonicalLookup("1", "Bug", false));
        lookups.Statuses.Should().HaveCount(2);
        lookups.Statuses.Single(s => s.ExternalId == "2").IsClosed.Should().BeTrue();
        lookups.Statuses.Single(s => s.ExternalId == "3").IsClosed.Should().BeFalse();
        lookups.Priorities.Should().ContainSingle().Which.IsClosed.Should().BeFalse();
    }

    [Fact]
    public void BuildOptionsResolver_ExtractsValueOrLabel_DropsBlanks_Distinct()
    {
        var definitions = new List<EpCustomFieldDefinition>
        {
            new(10, "Severity", "list",
            [
                new EpPossibleValue("Low", null),
                new EpPossibleValue(null, "High"),  // value null -> falls through to label "High"
                new EpPossibleValue("", "Ignored"), // value is "" (non-null) -> "" wins over ?? and is dropped as blank
                new EpPossibleValue("Low", null),   // duplicate -> distinct removes it
            ]),
            new(11, "Plain", "string", null),
        };

        var resolver = EasyProjectCanonicalMapper.BuildOptionsResolver(definitions);

        var options = resolver(10);
        options.Should().NotBeNull();
        options!.Should().BeEquivalentTo(["Low", "High"], o => o.WithStrictOrdering());

        resolver(11).Should().BeNull(); // no possible values
        resolver(999).Should().BeNull(); // unknown field id
    }

    [Fact]
    public void MapCustomField_UsesValueToStringAndResolverOptions()
    {
        var cf = new EpCustomField(10, "Severity", "High", Multiple: false, FieldFormat: "list");

        var mapped = EasyProjectCanonicalMapper.MapCustomField(cf, ["Low", "High"]);

        mapped.ExternalFieldId.Should().Be("10");
        mapped.Name.Should().Be("Severity");
        mapped.Value.Should().Be("High");
        mapped.Format.Should().Be(CanonicalFieldFormat.Select);
        mapped.Options.Should().BeEquivalentTo(["Low", "High"]);
    }

    [Fact]
    public void MapIssue_MapsCoreFields_FiltersEmptyJournals_FlattensChecklists()
    {
        var issue = new EpIssue(
            Id: 100,
            Subject: "Login broken",
            Description: "<p>HTML body</p>",
            Tracker: new EpRef(1, "Bug"),
            Status: new EpRef(2, "In Progress"),
            Priority: new EpRef(4, "High"),
            AssignedTo: new EpRef(7, "Jane"),
            Author: new EpRef(8, "John"),
            EstimatedHours: 3.5m,
            DoneRatio: 50,
            StartDate: "2026-01-01",
            DueDate: "2026-02-01",
            Parent: new EpRef(99, "Epic"),
            Project: new EpRef(50, "Web"),
            CustomFields: null,
            Journals:
            [
                new EpJournal(1, new EpRef(8, "John"), "Real comment", "2026-01-02T10:00:00Z", PrivateNotes: false),
                new EpJournal(2, new EpRef(8, "John"), "   ", "2026-01-02T11:00:00Z", PrivateNotes: false), // whitespace -> dropped
                new EpJournal(3, new EpRef(8, "John"), null, "2026-01-02T12:00:00Z", PrivateNotes: true),   // null -> dropped
            ],
            Attachments:
            [
                new EpAttachment(20, "spec.pdf", 1024, "application/pdf", "https://ep/att/20", null, "2026-01-03T00:00:00Z"),
            ],
            EasyChecklists:
            [
                new EpChecklist(30, "Steps",
                [
                    new EpChecklistItem(31, "First", Done: true, Position: 0),
                    new EpChecklistItem(32, "Second", Done: false, Position: 1),
                ]),
            ]);

        var mapped = EasyProjectCanonicalMapper.MapIssue(issue, _ => null);

        mapped.ExternalId.Should().Be("100");
        mapped.Title.Should().Be("Login broken");
        mapped.DescriptionHtml.Should().Be("<p>HTML body</p>");
        mapped.TypeExternalId.Should().Be("1");
        mapped.StatusExternalId.Should().Be("2");
        mapped.StatusName.Should().Be("In Progress");
        mapped.PriorityExternalId.Should().Be("4");
        mapped.Assignee!.ExternalId.Should().Be("7");
        mapped.Reporter!.ExternalId.Should().Be("8");
        mapped.EstimatedHours.Should().Be(3.5m);
        mapped.DueDate.Should().Be("2026-02-01");
        mapped.ParentExternalId.Should().Be("99");
        mapped.ProjectExternalId.Should().Be("50");
        mapped.ProjectName.Should().Be("Web");

        mapped.Comments.Should().ContainSingle();
        mapped.Comments[0].ExternalId.Should().Be("1");
        mapped.Comments[0].BodyHtml.Should().Be("Real comment");

        mapped.Attachments.Should().ContainSingle();
        mapped.Attachments[0].FileName.Should().Be("spec.pdf");
        mapped.Attachments[0].FileSizeBytes.Should().Be(1024);

        mapped.ChecklistItems.Should().HaveCount(2);
        mapped.ChecklistItems.Select(i => i.ExternalId).Should().BeEquivalentTo(["31", "32"]);
    }

    [Fact]
    public void MapIssue_Parses_UsFormat_UpdatedOn_AsInvariant()
    {
        // EasyProject returns "MM/dd/yyyy HH:mm:ss" — must be read as June 5 (month 6), not May 6,
        // regardless of the server's culture.
        var issue = new EpIssue(
            Id: 1, Subject: "x", Description: null,
            Tracker: null, Status: null, Priority: null, AssignedTo: null, Author: null,
            EstimatedHours: null, DoneRatio: null, StartDate: null, DueDate: null,
            Parent: null, Project: null, CustomFields: null, Journals: null, Attachments: null, EasyChecklists: null,
            UpdatedOn: "06/05/2026 06:11:17");

        var mapped = EasyProjectCanonicalMapper.MapIssue(issue, _ => null);

        mapped.SourceUpdatedAt.Should().Be(new DateTime(2026, 6, 5, 6, 11, 17, DateTimeKind.Utc));
    }

    [Fact]
    public void MapIssue_HandlesNullCollectionsAndRefs()
    {
        var issue = new EpIssue(
            Id: 101, Subject: "Minimal", Description: null,
            Tracker: null, Status: null, Priority: null, AssignedTo: null, Author: null,
            EstimatedHours: null, DoneRatio: null, StartDate: null, DueDate: null,
            Parent: null, Project: null, CustomFields: null, Journals: null, Attachments: null, EasyChecklists: null);

        var mapped = EasyProjectCanonicalMapper.MapIssue(issue, _ => null);

        mapped.StatusExternalId.Should().BeNull();
        mapped.StatusName.Should().BeNull();
        mapped.Assignee.Should().BeNull();
        mapped.Reporter.Should().BeNull();
        mapped.Comments.Should().BeEmpty();
        mapped.Attachments.Should().BeEmpty();
        mapped.ChecklistItems.Should().BeEmpty();
        mapped.CustomFields.Should().BeEmpty();
    }

    [Fact]
    public void MapWorklog_DefaultsBillableToFalse_WhenNull()
    {
        var withFlag = new EpTimeEntry(1, new EpRef(50, "Web"), new EpRef(100, "Issue"), new EpRef(7, "Jane"), 2.0m, "2026-01-05", "work", EasyIsBillable: true);
        var withoutFlag = new EpTimeEntry(2, null, null, null, 1.0m, null, null, EasyIsBillable: null);

        var mappedWith = EasyProjectCanonicalMapper.MapWorklog(withFlag);
        mappedWith.ExternalId.Should().Be("1");
        mappedWith.IssueExternalId.Should().Be("100");
        mappedWith.User!.ExternalId.Should().Be("7");
        mappedWith.Hours.Should().Be(2.0m);
        mappedWith.IsBillable.Should().BeTrue();

        var mappedWithout = EasyProjectCanonicalMapper.MapWorklog(withoutFlag);
        mappedWithout.IssueExternalId.Should().BeNull();
        mappedWithout.User.Should().BeNull();
        mappedWithout.IsBillable.Should().BeFalse();
    }
}
