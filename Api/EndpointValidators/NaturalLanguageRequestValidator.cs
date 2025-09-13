using FluentValidation;

public class NaturalLanguageRequestValidator : AbstractValidator<NaturalLanguageQueryRequest>
{
    public NaturalLanguageRequestValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty()
            .WithMessage("'{PropertyName}' is required.")
            .MinimumLength(5)
            .WithMessage("'{PropertyName}' must be at least {MinLength} characters long.")
            .MaximumLength(2000)
            .WithMessage("'{PropertyName}' must not exceed {MaxLength} characters.");

        RuleFor(x => x.DateRange).NotNull().WithMessage("'{PropertyName}' is required.");

        When(
            x => x.DateRange != null,
            () =>
            {
                RuleFor(x => x.DateRange)
                    .Custom(
                        (dateRange, context) =>
                        {
                            if (dateRange!.Start.Date > dateRange.End.Date)
                            {
                                context.AddFailure(
                                    nameof(dateRange.End),
                                    "End date must be on or after the start date."
                                );
                            }

                            var OneYearAgo = DateTime.UtcNow.Date.AddYears(-1);
                            if (dateRange.Start.Date < OneYearAgo)
                            {
                                context.AddFailure(
                                    nameof(dateRange.Start),
                                    "Start date cannot be more than 1 year in the past."
                                );
                            }
                        }
                    );
            }
        );
    }
}
