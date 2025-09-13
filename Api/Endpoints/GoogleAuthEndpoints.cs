using System.Net;
using datopus.Api.DTOs;
using datopus.Api.EndpointFilters;
using datopus.Api.Utilities.Auth;
using datopus.Application.Services;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.AspNetCore.Http.HttpResults;

namespace datopus.Api.Endpoints
{
    public static class GoogleAuthEndpoints
    {
        public static void Register(WebApplication app)
        {
            var endpoints = app.MapGroup("/auth/google").RequireAuthorization();
            endpoints
                .MapPost("/token", ExchangeToken)
                .AddEndpointFilter<InputValidatorFilter<GoogleOAuthExchangeToken>>();
            endpoints
                .MapPost("/refresh", RefreshToken)
                .AddEndpointFilter<InputValidatorFilter<GoogleOAuthRefreshToken>>();
        }

        public static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> RefreshToken(
            HttpContext httpContext,
            GoogleOAuthRefreshToken payload,
            GoogleOAuthService service
        )
        {
            var userClaims = ClaimsMapper.MapUserClaims(httpContext);

            if (userClaims?.UserId == null)
                return TypedResults.Problem(
                    title: "Unauthorized",
                    statusCode: (int)HttpStatusCode.Unauthorized
                );

            try
            {
                var tokenResponse = await service.RefreshToken(
                    userClaims.UserId,
                    payload.RefreshToken
                );

                return TypedResults.Ok(tokenResponse);
            }
            catch (TokenResponseException ex)
            {
                var errorDetails = new Dictionary<string, object?>
                {
                    ["code"] = ex.Error.Error,
                    ["description"] = ex.Error.ErrorDescription,
                    ["uri"] = ex.Error.ErrorUri,
                    ["message"] = ex.Message,
                    ["status"] = ex.StatusCode.HasValue
                        ? (int)ex.StatusCode
                        : (int)HttpStatusCode.Unauthorized,
                    ["timestamp"] = DateTime.UtcNow.ToString("o"),
                    ["instance"] = "/auth/google/refresh",
                };

                if (ex.Data?.Count > 0)
                {
                    errorDetails["data"] = ex.Data;
                }

                return TypedResults.Problem(
                    title: "GoogleTokenError",
                    extensions: errorDetails,
                    statusCode: ex.StatusCode != null
                        ? (int)ex.StatusCode
                        : (int)HttpStatusCode.Unauthorized,
                    type: "https://developers.google.com/identity/protocols/oauth2",
                    instance: "/auth/google/refresh"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: ", ex.Message.ToString());
                return TypedResults.Problem(
                    title: "InternalServerError",
                    statusCode: (int)HttpStatusCode.InternalServerError
                );
            }
        }

        public static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> ExchangeToken(
            HttpContext httpContext,
            GoogleOAuthExchangeToken payload,
            GoogleOAuthService service
        )
        {
            var userClaims = ClaimsMapper.MapUserClaims(httpContext);

            if (userClaims?.UserId == null)
                return TypedResults.Problem(
                    title: "Unauthorized",
                    statusCode: (int)HttpStatusCode.Unauthorized
                );

            try
            {
                var tokenResponse = await service.ExchangeAuthorizationCodeForTokens(
                    userClaims.UserId,
                    payload.Code,
                    payload.RedirectUri
                );

                return TypedResults.Ok(tokenResponse);
            }
            catch (TokenResponseException ex)
            {
                return TypedResults.Problem(
                    title: "GoogleTokenError",
                    detail: ex.Message,
                    statusCode: ex.StatusCode != null
                        ? (int)ex.StatusCode
                        : (int)HttpStatusCode.Unauthorized,
                    type: "https://developers.google.com/identity/protocols/oauth2",
                    instance: "/auth/google/token"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: ", ex.Message.ToString());
                return TypedResults.Problem(
                    title: "InternalServerError",
                    statusCode: (int)HttpStatusCode.InternalServerError
                );
            }
        }
    }
}
