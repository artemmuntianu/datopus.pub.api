using System.Text.Json;
using System.Text.Json.Serialization;

namespace datopus.Application.Converters.Google;

public class BQMatchTypeConverter : JsonConverter<BQMatchType>
{
    public override BQMatchType Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return reader.GetString() switch
        {
            "MATCH_EXACT" => BQMatchType.Exact,
            "NOT_MATCH_EXACT" => BQMatchType.NotExact,
            "MATCH_REGEX" => BQMatchType.MatchRegex,
            "NOT_MATCH_REGEX" => BQMatchType.NotMatchRegex,
            "BEGINS_WITH" => BQMatchType.BeginsWith,
            "ENDS_WITH" => BQMatchType.EndsWith,
            "CONTAINS" => BQMatchType.Contains,
            _ => throw new JsonException("Invalid value for BQMatchType"),
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        BQMatchType value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStringValue(value.ToString());
    }
}
