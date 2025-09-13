using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace datopus.Core.Entities.DbEntities
{
    [Table("onboarding_progress")]
    public class OnboardingProgress : BaseModel
    {
        [PrimaryKey("org_id", shouldInsert: true)]
        public long OrgId { get; set; }

        [Column("signup_completed_at")]
        public DateTime? SignupCompletedAt { get; set; }

        [Column("setup_analytics_completed_at")]
        public DateTime? SetupAnalyticsCompletedAt { get; set; }

        [Column("setup_bigquery_completed_at")]
        public DateTime? SetupBigqueryCompletedAt { get; set; }

        [Column("link_bigquery_completed_at")]
        public DateTime? LinkBigqueryCompletedAt { get; set; }

        [Column("connect_analytics_completed_at")]
        public DateTime? ConnectAnalyticsCompletedAt { get; set; }

        [Column("connect_bigquery_completed_at")]
        public DateTime? ConnectBigqueryCompletedAt { get; set; }
    }
}
