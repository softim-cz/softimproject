using SoftimProject.Application.Features.Migration.EasyProject.Models;
using SoftimProject.Application.Integrations;

namespace SoftimProject.Infrastructure.Services.EasyProject;

/// <summary>
/// Pure mapping of EasyProject API models onto the canonical model. Kept separate from
/// the connector (which does the HTTP orchestration) so the mapping stays side-effect
/// free and unit-testable. The status/field-format/option-extraction rules mirror what
/// <c>EasyProjectMigrationService</c> applies today, so routing the existing migration
/// through the canonical model later does not change behavior.
/// </summary>
public static class EasyProjectCanonicalMapper
{
    public static CanonicalProjectStatus MapProjectStatus(int epStatus) => epStatus switch
    {
        5 => CanonicalProjectStatus.Completed,
        9 => CanonicalProjectStatus.Archived,
        _ => CanonicalProjectStatus.Active
    };

    public static CanonicalFieldFormat MapFieldFormat(string? fieldFormat) => fieldFormat?.ToLowerInvariant() switch
    {
        "int" or "float" => CanonicalFieldFormat.Number,
        "date" => CanonicalFieldFormat.Date,
        "list" or "enumeration" => CanonicalFieldFormat.Select,
        _ => CanonicalFieldFormat.Text
    };

    public static CanonicalUserRef? MapUserRef(EpRef? user) =>
        user is null ? null : new CanonicalUserRef(user.Id.ToString(), user.Name);

    public static CanonicalUser MapUser(EpUser user) => new(
        user.Id.ToString(),
        user.Login,
        user.Firstname,
        user.Lastname,
        user.Mail,
        user.AvatarUrl);

    public static CanonicalLookups MapLookups(
        IEnumerable<EpTracker> trackers,
        IEnumerable<EpIssueStatus> statuses,
        IEnumerable<EpIssuePriority> priorities) => new(
            trackers.Select(t => new CanonicalLookup(t.Id.ToString(), t.Name, false)).ToList(),
            statuses.Select(s => new CanonicalLookup(s.Id.ToString(), s.Name, s.IsClosed)).ToList(),
            priorities.Select(p => new CanonicalLookup(p.Id.ToString(), p.Name, false)).ToList());

    public static CanonicalCustomFieldValue MapCustomField(EpCustomField cf, IReadOnlyList<string>? options) => new(
        cf.Id.ToString(),
        cf.Name,
        cf.Value?.ToString(),
        MapFieldFormat(cf.FieldFormat),
        options);

    public static IReadOnlyList<CanonicalCustomFieldValue> MapCustomFields(
        List<EpCustomField>? customFields,
        Func<int, IReadOnlyList<string>?> optionsFor) =>
        customFields is null
            ? []
            : customFields.Select(cf => MapCustomField(cf, optionsFor(cf.Id))).ToList();

    public static CanonicalComment MapComment(EpJournal journal) => new(
        journal.Id.ToString(),
        MapUserRef(journal.User),
        journal.Notes,
        journal.PrivateNotes,
        ParseDateTime(journal.CreatedOn));

    public static CanonicalAttachment MapAttachment(EpAttachment att) => new(
        att.Id.ToString(),
        att.Filename,
        att.Filesize,
        att.ContentType,
        att.ContentUrl,
        ParseDateTime(att.CreatedOn));

    public static CanonicalChecklistItem MapChecklistItem(EpChecklistItem item) => new(
        item.Id.ToString(),
        item.Subject,
        item.Position,
        item.Done);

    public static CanonicalProject MapProject(EpProject project, Func<int, IReadOnlyList<string>?> optionsFor) => new(
        project.Id.ToString(),
        project.Name,
        project.Description,
        MapProjectStatus(project.Status),
        project.Parent?.Id.ToString(),
        project.StartDate,
        project.DueDate,
        MapCustomFields(project.CustomFields, optionsFor));

    public static CanonicalIssue MapIssue(EpIssue issue, Func<int, IReadOnlyList<string>?> optionsFor) => new(
        issue.Id.ToString(),
        issue.Subject,
        issue.Description,
        issue.Tracker?.Id.ToString(),
        issue.Status?.Id.ToString(),
        issue.Status?.Name,
        issue.Priority?.Id.ToString(),
        MapUserRef(issue.AssignedTo),
        MapUserRef(issue.Author),
        issue.EstimatedHours,
        issue.DueDate,
        issue.Parent?.Id.ToString(),
        issue.Project?.Id.ToString(),
        issue.Project?.Name,
        MapCustomFields(issue.CustomFields, optionsFor),
        // Mirror the engine: only journals carrying notes become comments.
        (issue.Journals ?? [])
            .Where(j => !string.IsNullOrWhiteSpace(j.Notes))
            .Select(MapComment)
            .ToList(),
        (issue.Attachments ?? []).Select(MapAttachment).ToList(),
        (issue.EasyChecklists ?? [])
            .SelectMany(c => c.Items ?? [])
            .Select(MapChecklistItem)
            .ToList());

    public static CanonicalWorklog MapWorklog(EpTimeEntry te) => new(
        te.Id.ToString(),
        te.Issue?.Id.ToString(),
        MapUserRef(te.User),
        te.SpentOn,
        te.Hours,
        te.Comments,
        te.EasyIsBillable ?? false);

    /// <summary>
    /// Flattens the separate custom-field definitions into select-option lists keyed by
    /// EasyProject field id, matching the extraction the engine performs today
    /// (<c>value ?? label</c>, dropping blanks, distinct).
    /// </summary>
    public static Func<int, IReadOnlyList<string>?> BuildOptionsResolver(IEnumerable<EpCustomFieldDefinition> definitions)
    {
        var map = definitions
            .Where(d => d.PossibleValues is { Count: > 0 })
            .ToDictionary(
                d => d.Id,
                d => (IReadOnlyList<string>)d.PossibleValues!
                    .Select(p => p.Value ?? p.Label ?? "")
                    .Where(v => v != "")
                    .Distinct()
                    .ToList());
        return id => map.GetValueOrDefault(id);
    }

    private static DateTime? ParseDateTime(string? dt) =>
        string.IsNullOrWhiteSpace(dt) ? null : DateTime.TryParse(dt, out var d) ? d.ToUniversalTime() : null;
}
