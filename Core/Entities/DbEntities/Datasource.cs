using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace datopus.Core.Entities.DbEntities
{
    [Table("datasource")]
    public class Datasource : BaseModel
    {
        [PrimaryKey("id")]
        public long Id { get; set; }

        [Column("created_at", ignoreOnInsert: true)]
        public DateTime CreatedAt { get; set; }

        [Column("ga_property_id")]
        public string GaPropertyId { get; set; }

        [Column("ga_measurement_id")]
        public string GaMeasurementId { get; set; }

        [Column("unique_id")]
        public string UniqueId { get; set; }
    }
}
