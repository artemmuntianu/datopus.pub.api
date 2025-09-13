using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace datopus.Core.Entities.DbEntities;

[Table("subscription")]
public class Subscription : BaseModel
{
    [PrimaryKey("id")]
    [Column("id", ignoreOnInsert: true)]
    public long Id { get; set; }

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("org_id")]
    public long? OrgId { get; set; }

    [Column("stripe_subscription_id")]
    public string? StripeSubscriptionId { get; set; }

    [Column("stripe_customer_id")]
    public string? StripeCustomerId { get; set; }

    [Column("price_id")]
    public string? PriceId { get; set; }

    [Column("product_id")]
    public string? ProductId { get; set; }

    [Column("quantity")]
    public long? Quantity { get; set; }

    [Column("lookup_key")]
    public string? LookupKey { get; set; }

    [Column("currency")]
    public string? Currency { get; set; }

    [Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Column("end_date")]
    public DateTime? EndDate { get; set; }

    [Column("current_period_start")]
    public DateTime? CurrentPeriodStart { get; set; }

    [Column("current_period_end")]
    public DateTime? CurrentPeriodEnd { get; set; }

    [Column("mtu_limit_exceeded", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public bool MTULimitExceeded { get; set; } = false;

    [Column("plan_updated_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime? PlanUpdatedAt { get; set; }

    [Column("trial_started")]
    public DateTime? TrialStarted { get; set; }

    [Column("trial_ended")]
    public DateTime? TrialEnded { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("cancel_at_period_end")]
    public bool? CancelAtPeriodEnd { get; set; }

    [Column("canceled_at")]
    public DateTime? CanceledAt { get; set; }
}
