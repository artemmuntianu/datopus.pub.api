using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace datopus.Core.Entities.DbEntities
{
    [Table("landing_page_users")]
    public class LandingPageUser : BaseModel
    {
        [PrimaryKey("email")]
        public string? Email { get; set; }

        [Column("affiliate_requested")]
        public DateTime? AffiliateRequested { get; set; }

        [Column("startup_requested")]
        public DateTime? StartupRequested { get; set; }

        [Column("is_messaged")]
        public bool? IsMessaged { get; set; }
    }
}
