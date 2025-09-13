using System.Text.Json;

namespace datopus.Application.Middlewares;

public class JsonExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public JsonExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BadHttpRequestException ex) when (ex.InnerException is JsonException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/400",
                title = "Invalid request body",
                status = 400,
                detail = "The request body contains invalid JSON or incorrect values.",
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
}
