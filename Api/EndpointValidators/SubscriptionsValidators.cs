using datopus.Api.DTOs.Subscriptions;
using FluentValidation;

namespace datopus.Api.Validators.Subscriptions;

public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.CancelPath).NotEmpty().WithMessage("CancelPath is required.");
        RuleFor(x => x.SuccessPath).NotEmpty().WithMessage("SuccessPath is required.");
        RuleFor(x => x.PriceId).NotEmpty().WithMessage("PriceId is required.");
        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Must(currency => currency == "usd" || currency == "eur")
            .WithMessage("Currency must be 'usd' or 'eur'.");

        RuleFor(x => x.Quantity)
            .NotEmpty()
            .WithMessage("Quantity is required.")
            .InclusiveBetween(1, 30000000)
            .WithMessage("Quantity must be between 1,000 and 30,000,000.")
            .Must(quantity => quantity % 1 == 0)
            .WithMessage("Quantity must be a multiple of 1.");
        ;
    }
}

public class TaxEstimateRequestValidator : AbstractValidator<TaxEstimateRequest>
{
    public TaxEstimateRequestValidator()
    {
        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Must(currency => currency == "usd" || currency == "eur")
            .WithMessage("Currency must be 'usd' or 'eur'.");

        RuleFor(x => x.ProductId).NotEmpty().WithMessage("ProductId is required.");
        RuleFor(x => x.Quantity)
            .NotEmpty()
            .WithMessage("Quantity is required.")
            .InclusiveBetween(1, 30000000)
            .WithMessage("Quantity must be between 1,000 and 30,000,000.")
            .Must(quantity => quantity % 1 == 0)
            .WithMessage("Quantity must be a multiple of 1.");
        ;
        ;
    }
}

public class PortalSessionRequestValidator : AbstractValidator<PortalSessionRequest>
{
    public PortalSessionRequestValidator()
    {
        RuleFor(x => x.Action)
            .NotEmpty()
            .WithMessage("Action is required.")
            .Must(action => action == "cancel" || action == "update")
            .WithMessage("Action must be 'cancel' or 'update'.");
    }
}

public class ChangeSubscriptionRequestValidator : AbstractValidator<ChangeSubscriptionRequest>
{
    public ChangeSubscriptionRequestValidator()
    {
        RuleFor(x => x.PriceId).NotEmpty().WithMessage("PriceId is required.");
        RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required.");
        RuleFor(x => x.Quantity)
            .NotEmpty()
            .WithMessage("Quantity is required.")
            .InclusiveBetween(1, 30000000)
            .WithMessage("Quantity must be between 1,000 and 30,000,000.")
            .Must(quantity => quantity % 1 == 0)
            .WithMessage("Quantity must be a multiple of 1.");
        ;
    }
}

public class CancelSubscriptionRequestValidator : AbstractValidator<CancelSubscriptionRequest>
{
    public CancelSubscriptionRequestValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required.");
    }
}

public class ProratesEstimateRequestValidator : AbstractValidator<ProratesEstimateRequest>
{
    public ProratesEstimateRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("CustomerId is required.");
        RuleFor(x => x.CurrentPriceId).NotEmpty().WithMessage("CurrentPriceId is required.");
        RuleFor(x => x.NewPriceId).NotEmpty().WithMessage("NewPriceId is required.");
        RuleFor(x => x.SubscriptionId).NotEmpty().WithMessage("SubscriptionId is required.");
        RuleFor(x => x.Quantity)
            .NotEmpty()
            .WithMessage("Quantity is required.")
            .InclusiveBetween(1, 30000000)
            .WithMessage("Quantity must be between 1,000 and 30,000,000.")
            .Must(quantity => quantity % 1 == 0)
            .WithMessage("Quantity must be a multiple of 1.");
        ;
    }
}

public class InvoicePreviewRequestValidator : AbstractValidator<InvoicePreviewRequest>
{
    public InvoicePreviewRequestValidator()
    {
        RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency is required.");
        RuleFor(x => x.PriceId).NotEmpty().WithMessage("PriceId is required.");
        RuleFor(x => x.Quantity)
            .NotEmpty()
            .WithMessage("Quantity is required.")
            .InclusiveBetween(1, 30000000)
            .WithMessage("Quantity must be between 1,000 and 30,000,000.")
            .Must(quantity => quantity % 1 == 0)
            .WithMessage("Quantity must be a multiple of 1.");
        ;
    }
}
