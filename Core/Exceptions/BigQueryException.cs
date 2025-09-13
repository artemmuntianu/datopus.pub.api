using System.Text.Json;

namespace datopus.Core.Exceptions;

public class BQException : BaseException
{
    public int StatusCode { get; }
    public object? ErrorDetails { get; }

    public BQException(int statusCode, string errorContent)
        : base($"BQ API error ({statusCode})")
    {
        StatusCode = statusCode;

        try
        {
            ErrorDetails = JsonSerializer.Deserialize<object>(errorContent);
        }
        catch
        {
            ErrorDetails = errorContent;
        }
    }

    public BQException(int statusCode, object? errorContent)
        : base($"BQ API error ({statusCode})")
    {
        StatusCode = statusCode;
        ErrorDetails = errorContent;
    }
}
