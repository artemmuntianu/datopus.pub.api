using System.Text.Json;
using System.Text.Json.Serialization;

namespace datopus.Application.Converters.Google;

public class BQOperationConverter : JsonConverter<BQOperation>
{
    public override BQOperation Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return reader.GetString() switch
        {
            "EQUAL" => BQOperation.Equal,
            "NOT_EQUAL" => BQOperation.NotEqual,
            "LESS_THAN" => BQOperation.LessThan,
            "LESS_THAN_OR_EQUAL" => BQOperation.LessThanOrEqual,
            "GREATER_THAN" => BQOperation.GreaterThan,
            "GREATER_THAN_OR_EQUAL" => BQOperation.GreaterThanOrEqual,
            _ => throw new JsonException("Invalid value for BQOperation"),
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        BQOperation value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStringValue(value.ToString());
    }
}
