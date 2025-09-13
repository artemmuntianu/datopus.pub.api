using System.Net;
using datopus.Api.DTOs;
using datopus.Api.Utilities.Auth;
using datopus.Application.Services;
using datopus.Core.Enums.Constants;
using Microsoft.AspNetCore.Http.HttpResults;
using Supabase.Gotrue.Exceptions;

namespace datopus.Api.Endpoints
{
    public static class AdminEndpoints
    {
        public static void Register(WebApplication app)
        {
            var endpoints = app.MapGroup("/admin").RequireAuthorization();
            endpoints.MapPatch("/user", UpdateUser);
        }

        // TODO: add validation
        public static async Task<Results<Ok<Supabase.Gotrue.User>, ProblemHttpResult>> UpdateUser(
            string userId,
            HttpContext httpContext,
            UpdateUserPayload userData,
            AuthService auth,
            DbService db
        )
        {
            var userClaims = ClaimsMapper.MapUserClaims(httpContext);

            if (userClaims?.AppMetaDataClaims?.Role != UserRoles.SystemAdmin)
                return TypedResults.Problem(
                    title: "Forbidden",
                    statusCode: (int)HttpStatusCode.Forbidden
                );

            try
            {
                var user = await db.UpdateUserMetadata(
                    userId,
                    userData.AppMetaData?.ToDictionary(),
                    userData.UserMetaData?.ToDictionary()
                );
                return TypedResults.Ok(user);
            }
            catch (GotrueException e)
            {
                return TypedResults.Problem(e.Message, statusCode: e.StatusCode);
            }
            catch (Exception e)
            {
                return TypedResults.Problem(
                    e.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }
    }
}
