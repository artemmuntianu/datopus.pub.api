using System.Text.Json.Serialization;

namespace datopus.Api.DTOs.Subscriptions;

public class CheckoutRequest
{
    [JsonPropertyName("price_id")]
    public required string PriceId { get; set; }

    [JsonPropertyName("currency")]
    public required string Currency { get; set; }

    [JsonPropertyName("quantity")]
    public required int Quantity { get; set; }

    [JsonPropertyName("success_path")]
    public required string SuccessPath { get; set; }

    [JsonPropertyName("cancel_path")]
    public required string CancelPath { get; set; }
}

public class PortalSessionRequest
{
    [JsonPropertyName("return_path")]
    public required string ReturnPath { get; set; }

    [JsonPropertyName("action")]
    public required string Action { get; set; }
}

public class TaxEstimateRequest
{
    [JsonPropertyName("quantity")]
    public required long Quantity { get; set; }

    [JsonPropertyName("currency")]
    public required string Currency { get; set; }

    [JsonPropertyName("product_id")]
    public required string ProductId { get; set; }
}

public class ChangeSubscriptionRequest
{
    [JsonPropertyName("subscription_id")]
    public required string SubscriptionId { get; set; }

    [JsonPropertyName("price_id")]
    public required string PriceId { get; set; }

    [JsonPropertyName("quantity")]
    public required int Quantity { get; set; }
}

public class CancelSubscriptionRequest
{
    [JsonPropertyName("subscription_id")]
    public required string SubscriptionId { get; set; }
}

public class ProratesEstimateRequest
{
    [JsonPropertyName("subscription_id")]
    public required string SubscriptionId { get; set; }

    [JsonPropertyName("customer_id")]
    public required string CustomerId { get; set; }

    [JsonPropertyName("current_price_id")]
    public required string CurrentPriceId { get; set; }

    [JsonPropertyName("new_price_id")]
    public required string NewPriceId { get; set; }

    [JsonPropertyName("quantity")]
    public required long Quantity { get; set; }
}

public class InvoicePreviewRequest
{
    [JsonPropertyName("quantity")]
    public required long Quantity { get; set; }

    [JsonPropertyName("currency")]
    public required string Currency { get; set; }

    [JsonPropertyName("price_id")]
    public required string PriceId { get; set; }

    [JsonPropertyName("skip_billing_cycle_anchor")]
    public bool SkipBillingCycleAnchor { get; set; } = false;
}
