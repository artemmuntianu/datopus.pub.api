namespace datopus.Api.Utilities.HttpContextHelpers;


public static class HttpContextHelper
{
    public static string? GetClientIp(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? null;

        return ipAddress == "::1" ? null : ipAddress;
    }
}
