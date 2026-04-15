using System.Text.Json.Serialization;

namespace SoftimProject.Application.Features.Migration.EasyProject.Models;

public sealed record EpUser(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("login")] string? Login,
    [property: JsonPropertyName("firstname")] string? Firstname,
    [property: JsonPropertyName("lastname")] string? Lastname,
    [property: JsonPropertyName("mail")] string? Mail,
    [property: JsonPropertyName("status")] int? Status,
    [property: JsonPropertyName("avatar_url")] string? AvatarUrl);
