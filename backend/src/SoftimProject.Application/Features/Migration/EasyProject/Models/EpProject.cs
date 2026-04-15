using System.Text.Json.Serialization;

namespace SoftimProject.Application.Features.Migration.EasyProject.Models;

public sealed record EpProject(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("parent")] EpRef? Parent,
    [property: JsonPropertyName("start_date")] string? StartDate,
    [property: JsonPropertyName("due_date")] string? DueDate,
    [property: JsonPropertyName("custom_fields")] List<EpCustomField>? CustomFields);
