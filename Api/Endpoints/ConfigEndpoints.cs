using datopus.Application.Services;
using datopus.Core.Entities.Subscription;
using datopus.Core.Services.Subscription;
using Microsoft.AspNetCore.Http.HttpResults;

namespace datopus.Api.Endpoints
{
    public class GetConfigResp
    {
        public required bool IsTrackingEnabled { get; set; }

        public required bool IsRRWebTrackingEnabled { get; set; }

        public string? GaMeasurementId { get; set; }
    }

    public static class ConfigEndpoints
    {
        public static void Register(WebApplication app)
        {
            var endpoints = app.MapGroup("/config");
            endpoints.MapGet("/", GetConfig);
        }

        public static async Task<Results<Ok<GetConfigResp>, ProblemHttpResult>> GetConfig(
            string dsid,
            DbService db,
            UserSubscriptionService subscriptionService
        )
        {
            var trackConfig = await db.GetTrackConfig(dsid);
            if (trackConfig is null)
                return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound);

            string[] validSubscriptionStatus =
            [
                SubscriptionStatuses.Active,
                SubscriptionStatuses.Trial,
                SubscriptionStatuses.Startup,
            ];

            bool isTrackingEnabled =
                validSubscriptionStatus.Contains(trackConfig.SubscriptionStatus)
                && trackConfig.MtuLimitExceeded == false;

            bool IsRRWebTrackingEnabled =
                isTrackingEnabled
                && subscriptionService.HasAccess(
                    PriceLookupKeyParser.ParseOptional(trackConfig.LookupKey),
                    [PriceLookupKey.OptimizeMonthly]
                );

            var resp = new GetConfigResp
            {
                GaMeasurementId = trackConfig.MeasurementId,
                IsTrackingEnabled = isTrackingEnabled,
                IsRRWebTrackingEnabled = IsRRWebTrackingEnabled,
            };

            return TypedResults.Ok(resp);
        }
    }
}
