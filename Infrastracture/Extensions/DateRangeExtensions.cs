using datopus.Core.Entities;

public static class DateRangeExtensions
{
    public static List<DateTime> GetDatesInRange(this DateRange range)
    {
        var dates = new List<DateTime>();

        for (var start = range.Start.Date; start.Date <= range.End.Date; start = start.AddDays(1))
        {
            dates.Add(start);
        }

        return dates;
    }
}
