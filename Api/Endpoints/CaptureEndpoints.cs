using System.Net;
using System.Text.Json;
using datopus.Application.Exceptions;
using datopus.Application.Services.Google;
using datopus.Core.Exceptions;

namespace datopus.Api.Endpoints
{
    public static class CaptureEndpoints
    {
        public static void Register(WebApplication app)
        {
            var endpoints = app.MapGroup("/capture");
            endpoints.MapPost("/rrweb", UploadRRWebSnapshot).AllowAnonymous();
            endpoints.MapPost("/recorded-sessions", GetRecordedSessionsList).RequireAuthorization();
            endpoints.MapPost("/session-blob-snapshots", GetBlobSnapshots).RequireAuthorization();
        }

        public static async Task<IResult> UploadRRWebSnapshot(
            HttpRequest request,
            IGoogleBlobStorageService googleBlobservice
        )
        {
            RRWebEventPayloadDTO? payload = null;

            try
            {
                string contentType = request.ContentType?.Split(';')[0].Trim() ?? string.Empty;

                switch (contentType)
                {
                    case "application/json":
                        payload = await JsonSerializer.DeserializeAsync<RRWebEventPayloadDTO>(
                            request.Body,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                        break;

                    case "text/plain":
                        using (var reader = new StreamReader(request.Body))
                        {
                            var body = await reader.ReadToEndAsync();
                            if (!string.IsNullOrWhiteSpace(body))
                            {
                                payload = JsonSerializer.Deserialize<RRWebEventPayloadDTO>(
                                    body,
                                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                                );
                            }
                        }
                        break;

                    default:
                        return Results.Problem(
                            "Unsupported content type. Please use 'application/json' or 'text/plain' with JSON body.",
                            statusCode: StatusCodes.Status415UnsupportedMediaType
                        );
                }

                if (payload is null)
                {
                    return Results.BadRequest("Invalid or empty payload provided.");
                }

                await googleBlobservice.SaveSnapshotsAsync(payload);

                return Results.Ok("Snapshots uploaded successfully.");
            }
            catch (JsonException jsonEx)
            {
                return Results.BadRequest($"Invalid JSON format: {jsonEx.Message}");
            }
            catch (ArgumentException argEx)
            {
                return Results.BadRequest(argEx.Message);
            }
            catch (BlobAccessException)
            {
                return Results.Problem(
                    "Service temporary unavailable due to storage access issue.",
                    statusCode: StatusCodes.Status503ServiceUnavailable
                );
            }
            catch (BlobDataFormatException)
            {
                return Results.Problem(
                    "Internal error processing snapshot data format.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
            catch
            {
                return Results.Problem(
                    "An internal server error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        private static async Task<IResult> GetRecordedSessionsList(
            HttpContext httpContext,
            RecordedSessionsListPayloadDTO request,
            BQService bQService
        )
        {
            if (
                !httpContext.Request.Headers.TryGetValue("X-Google-Auth-Token", out var tokenValues)
                || string.IsNullOrEmpty(tokenValues)
            )
            {
                return Results.Problem(
                    "Missing or invalid X-Google-Auth-Token header.",
                    statusCode: (int)HttpStatusCode.Unauthorized
                );
            }

            try
            {
                var query =
                    @$"
                WITH session_events AS (
                SELECT
                    evt.user_pseudo_id AS user_id,
                    (SELECT value.int_value FROM UNNEST(evt.event_params) WHERE key = 'ga_session_id') AS session_id,
                    TIMESTAMP_MICROS(evt.event_timestamp) AS event_time,
                    evt.geo.country AS user_country,
                    evt.device.category AS device_type,
                    evt.device.operating_system AS device_os,
                    evt.device.web_info.browser AS device_browser,
                    (SELECT value.string_value FROM UNNEST(evt.event_params) WHERE key = 'page_location') AS page_url,
                    (SELECT value.string_value FROM UNNEST(evt.event_params) WHERE key = 'elemEvent') AS elem_event,
                    EXISTS (
                    SELECT 1 FROM UNNEST(evt.event_params) ep WHERE ep.key = 'rrweb_enabled'
                    ) AS rrweb_enabled
                FROM
                    `{request.ProjectId}.{request.DatasetId}.events_*` AS evt
                WHERE
                    _TABLE_SUFFIX BETWEEN '{request.DateRange!.Start:yyyyMMdd}' AND '{request.DateRange.End:yyyyMMdd}'
                    AND evt.event_name = 'feature_event'
                    AND (SELECT value.int_value FROM UNNEST(evt.event_params) WHERE key = 'ga_session_id') IS NOT NULL
                )

                , ranked_events AS (
                SELECT *,
                    ROW_NUMBER() OVER (PARTITION BY user_id, session_id ORDER BY event_time) AS row_num
                FROM session_events
                WHERE rrweb_enabled
                )

                SELECT
                user_id,
                session_id,
                MIN(event_time) AS rrweb_range_start,
                MAX(event_time) AS rrweb_range_end,
                ANY_VALUE(user_country) AS user_country,
                ANY_VALUE(device_type) AS device_type,
                ANY_VALUE(device_os) AS device_os,
                ANY_VALUE(device_browser) AS device_browser,
                MAX(IF(row_num = 1, page_url, NULL)) AS start_page_url,
                COUNTIF(elem_event = 'click') AS clicks_count,
                COUNTIF(elem_event = 'keydown') AS keypress_count
                FROM
                ranked_events
                GROUP BY
                user_id,
                session_id
                ORDER BY
                rrweb_range_start DESC;";

                var result = await bQService.Query(
                    query,
                    request.ProjectId!,
                    tokenValues.ToString()
                );

                return Results.Ok(result);
            }
            catch (BQException ex)
            {
                var details = ex.ErrorDetails;

                return TypedResults.Problem(
                    title: ex.Message,
                    statusCode: ex.StatusCode,
                    extensions: new Dictionary<string, object?> { ["detail"] = ex.ErrorDetails }
                );
            }
            catch (BaseException ex)
            {
                return TypedResults.Problem(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return TypedResults.Problem(
                    title: "InternalServerError",
                    statusCode: (int)HttpStatusCode.InternalServerError
                );
            }
        }

        private static async Task<IResult> GetBlobSnapshots(
            HttpContext httpContext,
            GetBlobSnapshotsPayloadDTO payload,
            IGoogleBlobStorageService googleBlobservice
        )
        {
            if (
                payload == null
                || payload.DateRange == null
                || string.IsNullOrWhiteSpace(payload.MeasurementId)
                || string.IsNullOrWhiteSpace(payload.SessionId)
            )
            {
                return Results.BadRequest("Invalid payload provided.");
            }

            try
            {
                var result = await googleBlobservice.FetchSnapshots(
                    payload.DateRange!,
                    payload.MeasurementId!,
                    payload.SessionId!
                );

                return Results.Ok(result);
            }
            catch (BlobNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (BlobAccessException)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (BlobDataFormatException)
            {
                return Results.Problem(
                    "Error processing blob data.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
            catch
            {
                return Results.Problem(
                    "An internal server error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }
    };
}
