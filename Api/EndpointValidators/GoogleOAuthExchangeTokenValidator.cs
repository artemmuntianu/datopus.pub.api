using datopus.Api.DTOs;
using FluentValidation;

namespace datopus.Api.Validators;

public class GoogleOAuthExchangeTokenValidator : AbstractValidator<GoogleOAuthExchangeToken>
{
    public GoogleOAuthExchangeTokenValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("{PropertyName} is required");
        RuleFor(x => x.RedirectUri).NotEmpty().WithMessage("{PropertyName} is required");
    }
}
