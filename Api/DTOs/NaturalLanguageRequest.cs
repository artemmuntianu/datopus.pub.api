using datopus.Core.Entities;

public class NaturalLanguageQueryRequest
{
    public string Question { get; set; } = string.Empty;

    public DateRange? DateRange { get; set; }
}
