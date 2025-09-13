namespace datopus.Api.DTOs.Subscriptions;

public record SessionResponse(string id);

public class TaxEstimateResponse
{
    public required long SubTotalAmount { get; set; }
    public required long TotalAmount { get; set; }

    public required long TaxAmountExclusive { get; set; }

    public required long TaxAmountInclusive { get; set; }
}

public class ProratedEstimateResponse
{
    public required long Subtotal { get; set; }

    public required long? Taxes { get; set; }

    public required long ProratedTotalDueToday { get; set; }
    public required long TotalDueNextBilling { get; set; }
    public required DateTime PeriodStart { get; set; }

    public required DateTime PeriodEnd { get; set; }

    public required DateTime DueDate { get; set; }
    public required DateTime? NextPaymentDate { get; set; }

    public List<InvoiceItemDto> Items { get; set; } = new();
}

public class EstimateResponse
{
    public required long Subtotal { get; set; }

    public required long? Taxes { get; set; }

    public required long TotalDueToday { get; set; }
    public required long TotalDueNextBilling { get; set; }
    public required DateTime PeriodStart { get; set; }

    public required DateTime PeriodEnd { get; set; }

    public required DateTime DueDate { get; set; }
    public required DateTime? NextPaymentDate { get; set; }

    public List<InvoiceItemDto> Items { get; set; } = new();
}

public class InvoiceItemDto
{
    public required string Description { get; set; }
    public long Amount { get; set; }
    public long? Quantity { get; set; }
    public required string Interval { get; set; }
    public bool IsProration { get; set; }
    public List<TieredPricingDto> Tiers { get; set; } = new();
}

public class TieredPricingDto
{
    public long? UpTo { get; set; }
    public long? UnitAmount { get; set; }
}

public record PortalSessionResponse(string Url);

public class PlanResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    public List<PriceResponse> Prices { get; set; } = new();
}

public class PriceResponse
{
    public string Id { get; set; } = string.Empty;
    public long? Amount { get; set; }

    public decimal? AmountDecimal { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Interval { get; set; } = "one_time";
    public string PricingType { get; set; } = "flat";
    public List<PriceTierResponse> Tiers { get; set; } = new();

    public Dictionary<string, List<PriceTierResponse>> CurrencyTiers { get; set; } = new();
}

public class PriceTierResponse
{
    public long? UpTo { get; set; }
    public long FlatAmount { get; set; }

    public decimal FlatAmountDecimal { get; set; }

    public long UnitAmount { get; set; }

    public decimal UnitAmountDecimal { get; set; }
}

public class SubscriptionCancelResponse
{
    public string Message { get; set; } = "";
    public required string SubscriptionId { get; set; }
    public required string Status { get; set; }
    public required DateTime? CancelAt { get; set; }
}

public class VerifyCheckoutResponse
{
    public string Message { get; set; } = "";

    public required string SubscriptionId { get; set; }
    public required string Status { get; set; }
}

public class SubscriptionResponse
{
    public required DateTime CreatedAt { get; set; }

    public required long? OrgId { get; set; }

    public required long LatestInvoicePaid { get; set; }

    public required string StripeSubscriptionId { get; set; }

    public required string StripeCustomerId { get; set; }

    public required string? PriceId { get; set; }

    public required string? ProductId { get; set; }

    public required long? Quantity { get; set; }

    public required string? Currency { get; set; }

    public required DateTime? StartDate { get; set; }

    public required DateTime? EndDate { get; set; }

    public required string? Status { get; set; }

    public required bool? CancelAtPeriodEnd { get; set; }

    public required DateTime? CanceledAt { get; set; }
}
