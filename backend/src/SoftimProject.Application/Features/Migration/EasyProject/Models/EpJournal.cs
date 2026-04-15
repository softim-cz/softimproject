using System.Text.Json.Serialization;

namespace SoftimProject.Application.Features.Migration.EasyProject.Models;

public sealed record EpJournal(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("user")] EpRef? User,
    [property: JsonPropertyName("notes")] string? Notes,
    [property: JsonPropertyName("created_on")] string? CreatedOn,
    [property: JsonPropertyName("private_notes")] bool PrivateNotes);
