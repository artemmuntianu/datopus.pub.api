using System.Net;
using datopus.Api.DTOs;
using datopus.Api.EndpointFilters;
using datopus.Api.Utilities.Auth;
using datopus.Application.Services;
using datopus.Core.Enums.Constants;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace datopus.Api.Endpoints
{
    public static class UserProfileEndpoints
    {
        public static void Register(WebApplication app)
        {
            var endpoints = app.MapGroup("user/{id:guid}/profile").RequireAuthorization();
            endpoints
                .MapPost("/image", UploadProfileImage)
                .DisableAntiforgery()
                .AddEndpointFilter<InputValidatorFilter<ProfileImage>>();
        }

        public static async Task<Results<Ok<string>, ProblemHttpResult>> UploadProfileImage(
            HttpContext httpContext,
            string id,
            [FromForm] ProfileImage profileImage,
            ProfileImageService profileImageService
        )
        {
            var userClaims = ClaimsMapper.MapUserClaims(httpContext);

            if (userClaims?.UserId == null)
                return Problem("Unauthorized", HttpStatusCode.Unauthorized);

            if (!IsAuthorized(userClaims.UserId, id, userClaims?.AppMetaDataClaims?.Role))
                return Problem("Forbidden", HttpStatusCode.Forbidden);

            try
            {
                var url = await profileImageService.ProcessAndUploadProfileImage(
                    profileImage.file,
                    id,
                    ImageConstants.ThumbnailSizes[^1]
                );
                return TypedResults.Ok(url);
            }
            catch (Exception ex)
            {
                return Problem(
                    $"An error occurred: {ex.Message}",
                    HttpStatusCode.InternalServerError
                );
            }
        }

        private static bool IsAuthorized(string userId, string pathId, string? role)
        {
            return userId == pathId || role == UserRoles.HomeAdmin;
        }

        private static ProblemHttpResult Problem(
            string message,
            HttpStatusCode statusCode = HttpStatusCode.BadRequest
        )
        {
            return TypedResults.Problem(message, statusCode: (int)statusCode);
        }
    }
}
