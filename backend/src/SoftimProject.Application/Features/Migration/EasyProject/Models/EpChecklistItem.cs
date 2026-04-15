using System.Text.Json.Serialization;

namespace SoftimProject.Application.Features.Migration.EasyProject.Models;

public sealed record EpChecklistItem(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("done")] bool Done,
    [property: JsonPropertyName("position")] int Position);
