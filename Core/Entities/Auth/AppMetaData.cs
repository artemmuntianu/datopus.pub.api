using System.Text.Json.Serialization;

namespace datopus.Core.Entities.Auth;

public class AppMetaData
{
    [JsonPropertyName("orgId")]
    public long OrgId { get; set; }

    [JsonPropertyName("orgType")]
    public string? OrgType { get; set; }

    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    [JsonPropertyName("providers")]
    public string[]? Providers { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("orgSubscription")]
    public string? OrgSubscription { get; set; }

    [JsonPropertyName("stripeSubscriptionId")]
    public string? SubscriptionId { get; set; }

    [JsonPropertyName("stripeCustomerId")]
    public string? SubscriptionCustomerId { get; set; }

    [JsonPropertyName("subscriptionStatus")]
    public string? SubscriptionStatus { get; set; }
}
