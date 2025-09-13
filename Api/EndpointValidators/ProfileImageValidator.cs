using datopus.Api.DTOs;
using FluentValidation;

namespace datopus.Api.Validators;

public class ProfileImageValidator : AbstractValidator<ProfileImage>
{
    private string[] AllowedMimeTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/bmp",
        "image/webp",
        "image/tiff",
        "image/svg+xml",
    ];

    public ProfileImageValidator()
    {
        RuleFor(x => x.file.Length)
            .NotNull()
            .LessThanOrEqualTo(5 * 1024 * 1024)
            .WithMessage("File size is larger than allowed");

        RuleFor(x => x.file.ContentType)
            .NotNull()
            .Must(x => AllowedMimeTypes.Contains(x))
            .WithMessage("File type is not allowed");
    }
}
