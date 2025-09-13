using Stripe;
using Supabase.Postgrest;

namespace datopus.Application.Services.Subscriptions;

public class SubscriptionDBService
{
    private readonly Supabase.Client _adminSupabase;

    public SubscriptionDBService()
    {
        _adminSupabase = new Supabase.Client(
            Environment.GetEnvironmentVariable("SUPABASE_URL")!,
            Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")!,
            new Supabase.SupabaseOptions { AutoRefreshToken = false }
        );
    }

    public DateTime GetFirstDayOfTheNextMonthUTCSeconds()
    {
        DateTime firstOfNextMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)
            .AddMonths(1)
            .ToUniversalTime();
        return firstOfNextMonth;
    }

    public async Task UpsertSubscriptionAsync(Core.Entities.DbEntities.Subscription subscription)
    {
        await _adminSupabase
            .From<Core.Entities.DbEntities.Subscription>()
            .Upsert(subscription, new QueryOptions { OnConflict = "org_id" });
    }

    public async Task UpsertSubscriptionAsync(Subscription subscription)
    {
        var lineItem = subscription.Items.Data.LastOrDefault();

        var orgIdString = subscription.Metadata.GetValueOrDefault("orgId");

        if (!long.TryParse(orgIdString, out var orgId))
        {
            throw new Exception("Unable to get org id from subscription metadata");
        }

        var sub = new Core.Entities.DbEntities.Subscription
        {
            StripeSubscriptionId = subscription.Id,
            StripeCustomerId = subscription.CustomerId,
            OrgId = orgId,
            PriceId = lineItem?.Price.Id,
            Quantity = lineItem?.Quantity,
            Currency = lineItem?.Price.Currency,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndedAt,
            CurrentPeriodStart = subscription.CurrentPeriodStart,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
            CanceledAt = subscription.CanceledAt,
            Status = subscription.Status,
            ProductId = lineItem?.Price.ProductId,
            LookupKey = lineItem?.Price.LookupKey,
            UpdatedAt = DateTime.UtcNow,
        };

        await UpsertSubscriptionAsync(sub);
    }

    public async Task UpdateSubscriptionAsync(Core.Entities.DbEntities.Subscription subscription)
    {
        await _adminSupabase
            .From<Core.Entities.DbEntities.Subscription>()
            .Where(x => x.OrgId == subscription.OrgId)
            .Update(subscription);
    }

    public async Task<Core.Entities.DbEntities.Subscription?> GetSubscriptionByOrgId(long orgId)
    {
        return await _adminSupabase
            .From<Core.Entities.DbEntities.Subscription>()
            .Select("*")
            .Where((sub) => sub.OrgId == orgId)
            .Single();
    }
}
