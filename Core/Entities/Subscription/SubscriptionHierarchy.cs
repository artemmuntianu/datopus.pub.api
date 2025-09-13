using System.Collections.ObjectModel;

namespace datopus.Core.Entities.Subscription;

public static class SubscriptionDefinitions
{
    public static readonly IReadOnlyDictionary<
        PriceLookupKey,
        IReadOnlyList<PriceLookupKey>
    > SubscriptionHierarchy;

    static SubscriptionDefinitions()
    {
        var hierarchy = new Dictionary<PriceLookupKey, IReadOnlyList<PriceLookupKey>>
        {
            [PriceLookupKey.CollectMonthly] = new[]
            {
                PriceLookupKey.CollectMonthly,
                PriceLookupKey.CollectYearly,
            },
            [PriceLookupKey.CollectYearly] = new[]
            {
                PriceLookupKey.CollectMonthly,
                PriceLookupKey.CollectYearly,
            },
            [PriceLookupKey.OptimizeMonthly] = new[]
            {
                PriceLookupKey.CollectMonthly,
                PriceLookupKey.CollectYearly,
                PriceLookupKey.OptimizeMonthly,
                PriceLookupKey.OptimizeYearly,
            },
            [PriceLookupKey.OptimizeYearly] = new[]
            {
                PriceLookupKey.CollectMonthly,
                PriceLookupKey.CollectYearly,
                PriceLookupKey.OptimizeMonthly,
                PriceLookupKey.OptimizeYearly,
            },
            [PriceLookupKey.ScaleMonthly] = new[]
            {
                PriceLookupKey.CollectMonthly,
                PriceLookupKey.CollectYearly,
                PriceLookupKey.OptimizeMonthly,
                PriceLookupKey.OptimizeYearly,
                PriceLookupKey.ScaleMonthly,
                PriceLookupKey.ScaleYearly,
            },
            [PriceLookupKey.ScaleYearly] = new[]
            {
                PriceLookupKey.CollectMonthly,
                PriceLookupKey.CollectYearly,
                PriceLookupKey.OptimizeMonthly,
                PriceLookupKey.OptimizeYearly,
                PriceLookupKey.ScaleMonthly,
                PriceLookupKey.ScaleYearly,
            },
        };

        SubscriptionHierarchy = new ReadOnlyDictionary<
            PriceLookupKey,
            IReadOnlyList<PriceLookupKey>
        >(hierarchy);
    }
}
