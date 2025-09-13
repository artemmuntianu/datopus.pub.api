using datopus.Api.DTOs.Subscriptions;
using Stripe;

namespace datopus.Application.Services.Subscriptions;

public class PlanService : IPlanService
{
    private readonly ProductService _productService;
    private readonly PriceService _priceService;

    public PlanService(ProductService productService, PriceService priceService)
    {
        _productService = productService;
        _priceService = priceService;
    }

    public async Task<PlanResponse?> GetPlanByIdAsync(string productId)
    {
        var product = await _productService.GetAsync(productId);

        if (product == null || product.Active == false)
            return null;

        var prices = await _priceService.ListAsync(
            new PriceListOptions
            {
                Active = true,
                Product = productId,
                Expand = new List<string>
                {
                    "data.tiers",
                    "data.currency_options",
                    "data.currency_options.usd.tiers",
                    "data.currency_options.eur.tiers",
                },
            }
        );

        return new PlanResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Metadata = product.Metadata,
            Prices = prices.Data.Select(MapPriceToResponse).ToList(),
        };
    }

    public async Task<IEnumerable<PlanResponse>?> GetPlansAsync()
    {
        var products = await _productService.ListAsync(new ProductListOptions { Active = true });

        if (products == null)
            return null;

        var prices = await _priceService.ListAsync(
            new PriceListOptions
            {
                Active = true,
                Expand = new List<string>
                {
                    "data.tiers",
                    "data.currency_options",
                    "data.currency_options.usd.tiers",
                    "data.currency_options.eur.tiers",
                },
            }
        );

        return products.Select(
            (product) =>
            {
                return new PlanResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Metadata = product.Metadata,
                    Prices = prices
                        .Data.Where(price => price.ProductId == product.Id)
                        .Select(MapPriceToResponse)
                        .ToList(),
                };
            }
        );
    }

    private PriceResponse MapPriceToResponse(Price price)
    {
        return new PriceResponse
        {
            Id = price.Id,
            Amount = price.UnitAmount,
            AmountDecimal = price.UnitAmountDecimal,
            Currency = price.Currency,
            Interval = price.Recurring.Interval,
            PricingType = price.TiersMode ?? "flat",
            Tiers =
                price.Tiers?.Select(MapTierToResponse).ToList() ?? new List<PriceTierResponse>(),
            CurrencyTiers =
                price.CurrencyOptions?.ToDictionary(
                    kvp => kvp.Key,
                    kvp =>
                        kvp.Value.Tiers != null
                            ? kvp.Value.Tiers.Select(MapCurrencyTierToResponse).ToList()
                            : new List<PriceTierResponse>
                            {
                                new PriceTierResponse
                                {
                                    UpTo = null,
                                    UnitAmount = kvp.Value.UnitAmount ?? 0,
                                    UnitAmountDecimal = kvp.Value.UnitAmountDecimal ?? 0,
                                    FlatAmount = kvp.Value.UnitAmount ?? 0,
                                    FlatAmountDecimal = kvp.Value.UnitAmountDecimal ?? 0,
                                },
                            }
                ) ?? new Dictionary<string, List<PriceTierResponse>>(),
        };
    }

    private PriceTierResponse MapTierToResponse(PriceTier tier)
    {
        return new PriceTierResponse
        {
            UpTo = tier.UpTo,
            UnitAmount = tier.UnitAmount ?? 0,
            UnitAmountDecimal = tier.UnitAmountDecimal ?? 0,
            FlatAmount = tier.FlatAmount ?? 0,
            FlatAmountDecimal = tier.FlatAmountDecimal ?? 0,
        };
    }

    private PriceTierResponse MapCurrencyTierToResponse(PriceCurrencyOptionsTier tier)
    {
        return new PriceTierResponse
        {
            UpTo = tier.UpTo,
            UnitAmount = tier.UnitAmount ?? 0,
            UnitAmountDecimal = tier.UnitAmountDecimal ?? 0,
            FlatAmount = tier.FlatAmount ?? 0,
            FlatAmountDecimal = tier.FlatAmountDecimal ?? 0,
        };
    }
}
