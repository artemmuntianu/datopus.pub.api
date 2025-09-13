using System.Text.Json;
using System.Text.Json.Serialization;

public class UnixMillisecondsDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.Number)
            return null;

        var milliseconds = reader.GetInt64();
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTime? value,
        JsonSerializerOptions options
    )
    {
        if (value.HasValue)
        {
            var milliseconds = new DateTimeOffset(value.Value).ToUnixTimeMilliseconds();
            writer.WriteNumberValue(milliseconds);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
