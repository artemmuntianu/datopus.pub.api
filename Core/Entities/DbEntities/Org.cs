using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace datopus.Core.Entities.DbEntities
{
    [Table("org")]
    public class Org : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("datasource_id")]
        public long? DatasourceId { get; set; }

        [Column("created_at", ignoreOnInsert: true)]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at", ignoreOnUpdate: true)]
        public DateTime? UpdatedAt { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("type")]
        public string Type { get; set; }

        [Column("subscription")]
        public string Subscription { get; set; }
    }
}
