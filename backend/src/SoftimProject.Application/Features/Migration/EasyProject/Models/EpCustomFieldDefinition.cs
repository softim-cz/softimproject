using System.Text.Json.Serialization;

namespace SoftimProject.Application.Features.Migration.EasyProject.Models;

public sealed record EpCustomFieldDefinition(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("field_format")] string? FieldFormat,
    [property: JsonPropertyName("possible_values")] List<EpPossibleValue>? PossibleValues);

public sealed record EpPossibleValue(
    [property: JsonPropertyName("value")] string? Value,
    [property: JsonPropertyName("label")] string? Label);
