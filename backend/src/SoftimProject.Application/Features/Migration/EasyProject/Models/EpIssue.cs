using System.Text.Json.Serialization;

namespace SoftimProject.Application.Features.Migration.EasyProject.Models;

public sealed record EpIssue(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("tracker")] EpRef? Tracker,
    [property: JsonPropertyName("status")] EpRef? Status,
    [property: JsonPropertyName("priority")] EpRef? Priority,
    [property: JsonPropertyName("assigned_to")] EpRef? AssignedTo,
    [property: JsonPropertyName("author")] EpRef? Author,
    [property: JsonPropertyName("estimated_hours")] decimal? EstimatedHours,
    [property: JsonPropertyName("done_ratio")] int? DoneRatio,
    [property: JsonPropertyName("start_date")] string? StartDate,
    [property: JsonPropertyName("due_date")] string? DueDate,
    [property: JsonPropertyName("parent")] EpRef? Parent,
    [property: JsonPropertyName("project")] EpRef? Project,
    [property: JsonPropertyName("custom_fields")] List<EpCustomField>? CustomFields,
    [property: JsonPropertyName("journals")] List<EpJournal>? Journals,
    [property: JsonPropertyName("attachments")] List<EpAttachment>? Attachments,
    [property: JsonPropertyName("easy_checklists")] List<EpChecklist>? EasyChecklists,
    [property: JsonPropertyName("updated_on")] string? UpdatedOn = null);

public sealed record EpChecklist(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("easy_checklist_items")] List<EpChecklistItem>? Items);
