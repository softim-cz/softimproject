using System.Text.Json.Serialization;

namespace SoftimProject.Application.Features.Migration.EasyProject.Models;

public sealed record EpCustomField(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("value")] object? Value,
    [property: JsonPropertyName("multiple")] bool? Multiple,
    [property: JsonPropertyName("field_format")] string? FieldFormat);
