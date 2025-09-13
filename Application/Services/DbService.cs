using datopus.Core.Entities.DbEntities;
using Supabase.Gotrue;
using System.Text.Json;

namespace datopus.Application.Services
{
    public class DbService(Supabase.Client sbClient)
    {
        // TODO: inject key from config;
        private const string SupabaseApiServiceRoleKey =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZxZXR2dGd2cHZ2ZWNrdGRtdmZ1Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTcyNDg0NjcxNCwiZXhwIjoyMDQwNDIyNzE0fQ.U9RUR16ai9JgxSRSmgGXFDkuVMEclI7NdEIWsQxMVq8";

        public async Task<OrgTrackerInfo?> GetTrackConfig(string unique_id)
        {
            try
            {
                var result = await sbClient.Rpc(
                    "get_tracker_config",
                    new Dictionary<string, string>(
                        [KeyValuePair.Create("unique_tracking_id", unique_id)]
                    )
                );

                if (result.ResponseMessage?.IsSuccessStatusCode == true && result.Content != null)
                {
                    var results = JsonSerializer.Deserialize<List<OrgTrackerInfo>>(result.Content);
                    return results?.FirstOrDefault();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Org> AddOrganization(Org org)
        {
            return (await sbClient.From<Org>().Insert(org)).Model!;
        }

        public async Task<OnboardingProgress> AddOnboardingProgress(OnboardingProgress onboardingProgress)
        {
            return (await sbClient.From<OnboardingProgress>().Insert(onboardingProgress)).Model!;
        }

        public async Task<LandingPageUser?> GetLandingPageUser(string email)
        {
            return await sbClient
                    .From<LandingPageUser>()
                    .Select("*")
                    .Where(x => x.Email == email)
                    .Single();
        }

        public async Task<User> UpdateUserMetadata(
            string userId,
            Dictionary<string, object>? appMetadata = null,
            Dictionary<string, object>? userMetadata = null
        )
        {
            var userAttrs = new AdminUserAttributes();

            if (appMetadata != null)
            {
                userAttrs.AppMetadata = appMetadata;
            }

            if (userMetadata != null)
            {
                userAttrs.UserMetadata = userMetadata;
            }

            return (
                await sbClient
                    .AdminAuth(Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")!)
                    .UpdateUserById(userId, userAttrs)
            )!;
        }
    }
}
