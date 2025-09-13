namespace datopus.Core.Entities.Subscription;

public static class SubscriptionStatuses
{
    public static readonly string Active = "active";

    public static readonly string Trial = "trial";

    public static readonly string Startup = "startup";

    public static readonly string Incomplete = "incomplete";
    public static readonly string IncompltedExpired = "incomplete_expired";

    public static readonly string PastDue = "past_due";
    public static readonly string Canceled = "canceled";
    public static readonly string Unpaid = "unpaid";

    public static readonly string Paused = "paused";
}
