using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoftimProject.Application.Features.Migration.EasyProject.Models;

/// <summary>
/// Reads a JSON value as a string even when the source emits it as a number or boolean.
/// EasyProject's custom-field <c>possible_values</c> mix string ids ("90") and numeric ids
/// (82, e.g. user-type fields), which would otherwise break deserialization.
/// </summary>
public sealed class FlexibleStringJsonConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var l)
                ? l.ToString(CultureInfo.InvariantCulture)
                : reader.GetDouble().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.Null => null,
            // Skip unexpected objects/arrays rather than throwing.
            _ => SkipAndNull(ref reader),
        };

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value);
    }

    private static string? SkipAndNull(ref Utf8JsonReader reader)
    {
        reader.Skip();
        return null;
    }
}
