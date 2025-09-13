using datopus.Api.DTOs;
using FluentValidation;

namespace datopus.Api.Validators;

public class GoogleOAuthRefreshTokenValidator : AbstractValidator<GoogleOAuthRefreshToken>
{
    public GoogleOAuthRefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("{PropertyName} is required");
    }
}
