using System.Text.Json.Serialization;

namespace SoftimProject.Application.Features.Migration.EasyProject.Models;

public sealed record EpTimeEntry(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("project")] EpRef? Project,
    [property: JsonPropertyName("issue")] EpRef? Issue,
    [property: JsonPropertyName("user")] EpRef? User,
    [property: JsonPropertyName("hours")] decimal Hours,
    [property: JsonPropertyName("spent_on")] string? SpentOn,
    [property: JsonPropertyName("comments")] string? Comments,
    [property: JsonPropertyName("easy_is_billable")] bool? EasyIsBillable);
