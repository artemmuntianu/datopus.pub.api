using System.Text.Json.Serialization;

namespace datopus.Core.Entities;

public class DateRange
{
    [JsonPropertyName("start")]
    public DateTime Start { get; private set; }

    [JsonPropertyName("end")]
    public DateTime End { get; private set; }

    public DateRange(DateTime start, DateTime end)
    {
        if (start > end)
        {
            throw new ArgumentException("Start date must be before or equal to end date.");
        }
        Start = start;
        End = end;
    }

    public static DateRange LastDays(int days)
    {
        if (days < 0)
            throw new ArgumentException("Days must be non-negative.");

        DateTime end = DateTime.UtcNow.Date;
        DateTime start = end.AddDays(-days);
        return new DateRange(start, end);
    }

    public override string ToString()
    {
        return $"DateRange: {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}";
    }
}
