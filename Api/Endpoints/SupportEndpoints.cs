using System.Net;
using datopus.api.Core.Services;
using datopus.Api.DTOs.SupportRequests;
using datopus.Api.EndpointFilters;
using datopus.Api.Utilities.Auth;
using datopus.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace datopus.Api.Endpoints
{
    public static class SupportEndpoints
    {
        public static void Register(WebApplication app)
        {
            var endpoints = app.MapGroup("/support");
            endpoints
                .MapPost("/request", SupportRequest)
                .RequireAuthorization()
                .DisableAntiforgery()
                .AddEndpointFilter<InputValidatorFilter<SupportRequest>>();
        }

        private static async Task<IResult> SupportRequest(
            [FromForm] SupportRequest request,
            HttpContext httpContext,
            ILogger<SupportRequest> logger
        )
        {
            var userClaims = ClaimsMapper.MapUserClaims(httpContext);

            if (string.IsNullOrWhiteSpace(userClaims?.UserMetaDataClaims?.Email))
            {
                logger.LogWarning(
                    "Invalid user: Empty email address. User: {User}",
                    userClaims?.UserMetaDataClaims?.FullName
                );
                return Results.BadRequest(
                    new
                    {
                        errors = new Dictionary<string, string[]>
                        {
                            { "Email", new[] { "Empty email address." } },
                        },
                    }
                );
            }
            try
            {
                logger.LogInformation(
                    "Sending support request. User: {User}, Subject: {Subject}",
                    userClaims.UserMetaDataClaims.FullName,
                    request.Subject
                );

                await EmailService.SendSupportRequest(
                    userClaims.UserMetaDataClaims.Email,
                    userClaims.UserMetaDataClaims.EmailVerified,
                    userClaims.UserMetaDataClaims.FullName,
                    userClaims.UserMetaDataClaims.OrgName,
                    request.Subject!,
                    request.Message!,
                    request.AllowProjectSupport,
                    request.Screenshots?.ToArray()
                );

                logger.LogInformation(
                    "Support request sent successfully. User: {User}, Subject: {Subject}",
                    userClaims.UserMetaDataClaims.FullName,
                    request.Subject
                );

                return Results.Ok();
            }
            catch (EmailServiceException emailEx)
            {
                logger.LogError(
                    emailEx,
                    "Error sending email for user: {User}, Subject: {Subject}",
                    userClaims.UserMetaDataClaims.FullName,
                    request.Subject
                );
                return Results.Problem(
                    "Failed to send the support request email. Please try again later.",
                    statusCode: (int)HttpStatusCode.BadGateway
                );
            }
            catch (ArgumentNullException argEx)
            {
                logger.LogError(
                    argEx,
                    "Missing required arguments for support request. User: {User}, Subject: {Subject}",
                    userClaims.UserMetaDataClaims.FullName,
                    request.Subject
                );

                return Results.BadRequest("Required data missing in the support request.");
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unexpected error while processing support request. User: {User}, Subject: {Subject}",
                    userClaims.UserMetaDataClaims.FullName,
                    request.Subject
                );
                return Results.Problem(
                    "An unexpected error occurred while processing the support request.",
                    statusCode: (int)HttpStatusCode.InternalServerError
                );
            }
        }
    }
}
