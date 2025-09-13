using datopus.Api.DTOs;
using FluentValidation;
using FluentValidation.Results;

namespace datopus.Api.Validators;

public class BQQueryValidator : AbstractValidator<BQQuery>
{
    public BQQueryValidator()
    {
        RuleFor(x => x.DateRange)
            .NotEmpty()
            .WithMessage("{PropertyName} is required")
            .Custom(
                (range, context) =>
                {
                    if (range.Start.Date > range.End.Date)
                    {
                        context.AddFailure(
                            new ValidationFailure(
                                "DateRange",
                                "Start date must be lower or equal end date"
                            )
                        );
                    }
                    ;
                }
            );
    }
}
