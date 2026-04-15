using System.Text.Json.Serialization;

namespace SoftimProject.Application.Features.Migration.EasyProject.Models;

public sealed record EpTracker(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name);
