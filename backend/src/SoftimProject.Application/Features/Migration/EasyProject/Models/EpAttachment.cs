using System.Text.Json.Serialization;

namespace SoftimProject.Application.Features.Migration.EasyProject.Models;

public sealed record EpAttachment(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("filesize")] long Filesize,
    [property: JsonPropertyName("content_type")] string? ContentType,
    [property: JsonPropertyName("content_url")] string ContentUrl,
    [property: JsonPropertyName("author")] EpRef? Author,
    [property: JsonPropertyName("created_on")] string? CreatedOn);
