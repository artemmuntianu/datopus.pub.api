namespace datopus.Application.Services
{
    public class BQDashboardService
    {
        private readonly Supabase.Client _adminSupabase;

        public BQDashboardService()
        {
            _adminSupabase = new Supabase.Client(
                Environment.GetEnvironmentVariable("SUPABASE_URL")!,
                Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")!,
                new Supabase.SupabaseOptions { AutoRefreshToken = false }
            );
        }

        public async Task AddDefaultDashboard(long orgId, string userId)
        {
            await _adminSupabase.Rpc(
                "add_system_dashboard",
                new
                {
                    dashboard = new
                    {
                        name = "Google Big Query",
                        description = "Default big query dashboard with predefined configuration",
                        org_id = orgId,
                        author_id = userId,
                    },
                    report_system_names = new[] { "events", "usage", "flow" },
                    dashboard_tiles_config = new[]
                    {
                        new
                        {
                            name = "Events",
                            description = "",
                            width = 2,
                            height = 2,
                            x = 0,
                            y = 0,
                        },
                        new
                        {
                            name = "Usage",
                            description = "",
                            width = 2,
                            height = 2,
                            x = 0,
                            y = 2,
                        },
                        new
                        {
                            name = "Flow",
                            description = "",
                            width = 2,
                            height = 2,
                            x = 2,
                            y = 0,
                        },
                    },
                }
            );
        }
    }
}
