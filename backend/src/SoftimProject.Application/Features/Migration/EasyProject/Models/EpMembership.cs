using System.Text.Json.Serialization;

namespace SoftimProject.Application.Features.Migration.EasyProject.Models;

public sealed record EpMembership(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("project")] EpRef? Project,
    [property: JsonPropertyName("user")] EpRef? User,
    [property: JsonPropertyName("roles")] List<EpRef>? Roles);
