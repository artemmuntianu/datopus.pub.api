using System.Net;
using System.Text;
using System.Text.Json;
using datopus.Api.DTOs;
using datopus.Api.EndpointFilters;
using datopus.Api.Utilities.Auth;
using datopus.Application.Services.Google;
using datopus.Core.Entities;
using datopus.Core.Entities.BigQuery;
using datopus.Core.Entities.Subscription;
using datopus.Core.Exceptions;
using datopus.Core.Services.Subscription;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace datopus.Api.Endpoints
{
    public static class BigQueryEndpoints
    {
        public static void Register(WebApplication app)
        {
            var endpoints = app.MapGroup("/bigquery").RequireAuthorization();
            endpoints
                .MapPost("/{projectId}/{propertyId}/{tableId}", Query)
                .AddEndpointFilter<InputValidatorFilter<BQQuery>>();
            endpoints
                .MapPost("/{projectId}/{propertyId}/{tableId}/nlq", NaturalLanguageQuery)
                .AddEndpointFilter<InputValidatorFilter<NaturalLanguageQueryRequest>>();
            endpoints.MapGet("/projects/{projectId}", GetProject);
        }

        public static async Task<Results<Ok<object>, ProblemHttpResult>> Query(
            HttpContext httpContext,
            BQQuery payload,
            string projectId,
            string propertyId,
            string tableId,
            BQService service,
            UserSubscriptionService subscriptionService,
            BQBuilder builder
        )
        {
            var userClaims = ClaimsMapper.MapUserClaims(httpContext);

            if (
                !subscriptionService.HasAccess(
                    PriceLookupKeyParser.ParseOptional(
                        userClaims?.AppMetaDataClaims?.OrgSubscription
                    ),
                    [PriceLookupKey.OptimizeMonthly]
                )
            )
                return TypedResults.Problem(statusCode: (int)HttpStatusCode.Forbidden);

            if (
                !httpContext.Request.Headers.TryGetValue("X-Google-Auth-Token", out var tokenValues)
                || string.IsNullOrEmpty(tokenValues)
            )
            {
                return TypedResults.Problem(
                    "Missing or invalid X-Google-Auth-Token header.",
                    statusCode: (int)HttpStatusCode.Unauthorized
                );
            }
            try
            {
                var query = builder.CreateQuery(
                    payload.Metrics,
                    payload.Dimensions,
                    payload.DateRange,
                    payload.MetricFilter,
                    payload.DimensionFilter,
                    payload.OrderBys,
                    projectId,
                    propertyId,
                    tableId
                );

                var result = await service.Query(query, projectId, tokenValues.ToString());

                return TypedResults.Ok(result);
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

        public static async Task<object> GetProject(
            HttpContext httpContext,
            BQService service,
            string projectId
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

            if (string.IsNullOrEmpty(projectId))
            {
                return Results.Problem(
                    "Invalid request: Missing project ID.",
                    statusCode: (int)HttpStatusCode.BadRequest
                );
            }
            try
            {
                var result = await service.GetProject(projectId, tokenValues.ToString());

                return TypedResults.Ok(result);
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

        public static async Task<IResult> NaturalLanguageQuery(
            HttpContext httpContext,
            [FromRoute] string projectId,
            [FromRoute] string propertyId,
            [FromRoute] string tableId,
            [FromBody] NaturalLanguageQueryRequest requestBody,
            BQService service,
            UserSubscriptionService subscriptionService,
            BQBuilder builder
        )
        {
            var userClaims = ClaimsMapper.MapUserClaims(httpContext);

            if (
                !subscriptionService.HasAccess(
                    PriceLookupKeyParser.ParseOptional(
                        userClaims?.AppMetaDataClaims?.OrgSubscription
                    ),
                    [PriceLookupKey.OptimizeMonthly]
                )
            )
                return TypedResults.Problem(statusCode: (int)HttpStatusCode.Forbidden);

            if (
                !httpContext.Request.Headers.TryGetValue("X-Google-Auth-Token", out var tokenValues)
                || string.IsNullOrEmpty(tokenValues)
            )
            {
                return TypedResults.Problem(
                    "Missing or invalid X-Google-Auth-Token header.",
                    statusCode: (int)HttpStatusCode.Unauthorized
                );
            }
            try
            {
                string aiResp = await GeminiSqlGenerator.GenerateSQLFromQuestion(
                    requestBody.Question,
                    requestBody.DateRange!,
                    propertyId,
                    projectId,
                    tableId
                );

                aiResp = aiResp.Replace("```json", "").Replace("```", "");
                var aiRespJson = JObject.Parse(aiResp);

                bool shouldSkipSQL = aiRespJson["skip_sql"]?.Value<bool>() ?? false;

                if (!shouldSkipSQL)
                {
                    var query = aiRespJson["sql"]?.ToString();
                    if (string.IsNullOrWhiteSpace(query))
                    {
                        return TypedResults.Problem(
                            title: "AI response indicated SQL execution but provided no SQL.",
                            statusCode: (int)HttpStatusCode.BadGateway
                        );
                    }

                    var bqResp = await service.Query(query, projectId, tokenValues.ToString());
                    var bqRespStr = JsonSerializer.Serialize(bqResp, typeof(object));
                    aiRespJson.Add("data", JToken.Parse(bqRespStr));
                }

                var jsonString = aiRespJson.ToString(Newtonsoft.Json.Formatting.None);
                return Results.Content(jsonString, "application/json");
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
    }

    public static class GeminiSqlGenerator
    {
        private static string GEMINI_API_KEY = Environment.GetEnvironmentVariable(
            "GOOGLE_GEMINI_API_KEY"
        )!;

        public static async Task<string> GenerateSQLFromQuestion(
            string question,
            DateRange range,
            string datasetId,
            string projectId,
            string tableId
        )
        {
            var prompt =
                $@"
            You are working with a BigQuery table `{projectId}.{datasetId}.{tableId}`.
            {GetSchemaHint()}.
            The table contains Google Analytics 4 (GA4) exported events.

            Your task is to generate an SQL query that answers the following question: ""{question}"".
            If question is maladaptive to SQL query, respond with explanation only and set flag skip_sql = true 
            Use the `_TABLE_SUFFIX` field to filter data between '{range.Start:yyyyMMdd}' and '{range.End:yyyyMMdd}'.

            Return a JSON object with the following fields:
            2. `skip_sql — true or false if question is maladaptive
            1. `sql` — a valid BigQuery SQL query (as valid sql string, without explanations);
            2. `supported_view_candidates` — an array of strings from the set ['chart', 'table'], sorted by best suitability;
            3. `chart_config` (only if 'chart' is in supported_view_candidates) — a JSON object describing the structure of the chart, with:
            - `supported_types`: an array of strings from the set ['line', 'bar', 'column', 'funnel'], sorted by best suitability;
            - `x_axis`: name of the column to be used for the x-axis (typically a timestamp, date, or category),
            - `y_axis`: name(s) of the column(s) to be used for y-axis (one or more numeric metrics),
            - `z_axis` (optional): name of the column for segmentation (e.g., user type, device, country);
            4. `table_columns` (only if 'table' is in supported_view_candidates) — array of column names to be displayed in the table view.
            5. `explanation` — a brief non-technical explanation to a user about the data we query (1-2 sentences);

            All field names in the response must exactly match the column names from the query output.
            ";
            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
            };

            using var httpClient = new HttpClient();
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );
            var response = await httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite:generateContent?key={GEMINI_API_KEY}",
                content
            );
            var responseString = await response.Content.ReadAsStringAsync();
            var obj = JObject.Parse(responseString);
            return obj["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()
                ?? "[SQL generation error]";
        }

        public static string GetSchemaHint()
        {
            return @"
            The table schema represents GA4 event data exported to BigQuery. Key fields include:

            **Core Event Information:**
            - `event_date` (STRING): Date of the event in YYYYMMDD format. Used for partitioning via _TABLE_SUFFIX.
            - `event_timestamp` (INT64): Timestamp of the event in microseconds since epoch. Convert to TIMESTAMP using `TIMESTAMP_MICROS(event_timestamp)`.
            - `event_name` (STRING): The name of the event (e.g., 'page_view', 'purchase', 'add_to_cart').
            - `platform` (STRING): Platform from which the event was logged ('WEB', 'IOS', 'ANDROID').

            **User Identifiers:**
            - `user_pseudo_id` (STRING): Anonymous identifier for the browser/device. Persistent across sessions.
            - `user_id` (STRING): Identifier assigned via the User-ID feature (if implemented). Null if not set.
            - `user_first_touch_timestamp` (INT64): Timestamp (microseconds) of the user's first interaction.

            **User Lifetime Value (LTV):**
            - `user_ltv.revenue` (FLOAT64): Total lifetime revenue for this user.
            - `user_ltv.currency` (STRING): Currency code for the revenue (e.g., 'USD').

            **Session Information (Accessed via event_params):**
            *   Session details are typically found within `event_params`. You MUST use UNNEST to access them.
            *   Example: `(SELECT value.int_value FROM UNNEST(event_params) WHERE key = 'ga_session_id') AS ga_session_id`
            *   Example: `(SELECT value.int_value FROM UNNEST(event_params) WHERE key = 'ga_session_number') AS ga_session_number`

            **Geography:**
            - `geo.continent` (STRING)
            - `geo.country` (STRING)
            - `geo.region` (STRING)
            - `geo.city` (STRING)
            - `geo.sub_continent` (STRING)
            - `geo.metro` (STRING)

            **Device:**
            - `device.category` (STRING): 'desktop', 'mobile', 'tablet'.
            - `device.mobile_brand_name` (STRING)
            - `device.mobile_model_name` (STRING)
            - `device.operating_system` (STRING)
            - `device.operating_system_version` (STRING)
            - `device.browser` (STRING)
            - `device.browser_version` (STRING)
            - `device.language` (STRING): Preferred language setting of the browser/device.
            - `device.web_info.browser` (STRING)
            - `device.web_info.browser_version` (STRING)
            - `device.web_info.hostname` (STRING)

            **Traffic Source:**
            - `traffic_source.name` (STRING): Campaign name.
            - `traffic_source.medium` (STRING): e.g., 'organic', 'cpc', 'referral'.
            - `traffic_source.source` (STRING): e.g., 'google', 'facebook.com', 'direct'.

            **Application Information (for mobile apps):**
            - `app_info.id` (STRING): Bundle ID (iOS) or Package Name (Android).
            - `app_info.version` (STRING): App version.
            - `app_info.install_source` (STRING)

            **Event Parameters (REPEATED RECORD):**
            - `event_params` (ARRAY<STRUCT<key STRING, value STRUCT<string_value STRING, int_value INT64, float_value FLOAT64, double_value FLOAT64>>>):
              Contains custom parameters and automatically collected parameters for the event.
            - Access values using UNNEST and specifying the value type.
            - **Common Keys:** 'page_location', 'page_referrer', 'page_title', 'ga_session_id', 'ga_session_number', 'campaign', 'medium', 'source', 'term', 'content', 'transaction_id', 'value', 'currency', 'firebase_screen_class', 'firebase_screen_id', etc.
            - **Example Query Syntax:**
              `SELECT event_name, (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'page_title') AS page_title, (SELECT value.int_value FROM UNNEST(event_params) WHERE key = 'ga_session_id') AS ga_session_id, (SELECT value.double_value FROM UNNEST(event_params) WHERE key = 'value') AS event_value FROM your_table`

            **User Properties (REPEATED RECORD):**
            - `user_properties` (ARRAY<STRUCT<key STRING, value STRUCT<string_value STRING, int_value INT64, float_value FLOAT64, double_value FLOAT64, set_timestamp_micros INT64>>>):
              Contains custom user properties you have defined.
            - Access values using UNNEST similar to `event_params`.
            - **Example Query Syntax:**
              `SELECT user_pseudo_id, (SELECT value.string_value FROM UNNEST(user_properties) WHERE key = 'user_segment') AS user_segment FROM your_table`

            **E-commerce Items (REPEATED RECORD):**
            - `items` (ARRAY<STRUCT<item_id STRING, item_name STRING, item_brand STRING, item_variant STRING, item_category STRING, item_category2 STRING, item_category3 STRING, item_category4 STRING, item_category5 STRING, price_in_usd FLOAT64, price FLOAT64, quantity INT64, item_revenue_in_usd FLOAT64, item_revenue FLOAT64, item_refund_in_usd FLOAT64, item_refund FLOAT64, coupon STRING, affiliation STRING, location_id STRING, item_list_id STRING, item_list_name STRING, item_list_index INT64, promotion_id STRING, promotion_name STRING, creative_name STRING, creative_slot STRING >>):
              Contains details about items associated with e-commerce events (e.g., 'add_to_cart', 'purchase', 'view_item').
            - Often requires UNNESTing both the `items` array and potentially `event_params` within the same query.
            - **Example Query Syntax:**
              `SELECT event_name, item.item_name, item.price FROM your_table, UNNEST(items) AS item WHERE event_name = 'purchase'`

            **IMPORTANT:** Always use `_TABLE_SUFFIX` to filter by date range for cost and performance efficiency.
        ";
        }
    }
}
