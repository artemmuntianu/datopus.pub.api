using System.Security.Claims;
using System.Text.Json;
using datopus.Core.Entities.Auth;

namespace datopus.Api.Utilities.Auth
{
    public static class ClaimsMapper
    {
        public static UserClaims? MapUserClaims(HttpContext httpContext)
        {
            var user = httpContext.User;

            if (user == null)
            {
                return null;
            }

            return new UserClaims
            {
                UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                AppMetaDataClaims = GetAppMetaDataClaims(user),
                UserMetaDataClaims = GetUserMetaDataClaims(user),
            };
        }

        private static AppMetaData? GetAppMetaDataClaims(ClaimsPrincipal user)
        {
            var appMetaDataClaim = user.FindFirst("app_metadata")?.Value;

            if (appMetaDataClaim == null)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<AppMetaData>(appMetaDataClaim);
            }
            catch
            {
                return null;
            }
        }

        private static UserMetaData? GetUserMetaDataClaims(ClaimsPrincipal user)
        {
            var userMetaDataClaim = user.FindFirst("user_metadata")?.Value;

            if (userMetaDataClaim == null)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<UserMetaData>(userMetaDataClaim);
            }
            catch
            {
                return null;
            }
        }
    }
}
