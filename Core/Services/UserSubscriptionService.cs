using datopus.Core.Entities.Subscription;

namespace datopus.Core.Services.Subscription;

public class UserSubscriptionService
{
    public bool HasAccess(
        PriceLookupKey? userSubscriptionKey,
        IEnumerable<PriceLookupKey> requiredSubscriptions,
        bool exact = false
    )
    {
        if (userSubscriptionKey is not PriceLookupKey key)
        {
            return false;
        }

        if (requiredSubscriptions == null)
        {
            return true;
        }

        if (exact)
        {
            return requiredSubscriptions.Contains(key);
        }

        if (
            SubscriptionDefinitions.SubscriptionHierarchy.TryGetValue(
                key,
                out var allowedSubscriptions
            )
        )
        {
            return requiredSubscriptions.Any(requiredSub =>
                allowedSubscriptions.Contains(requiredSub)
            );
        }
        else
        {
            return false;
        }
    }
}
