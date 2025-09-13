using datopus.Api.DTOs.SupportRequests;
using FluentValidation;

public class SupportRequestValidator : AbstractValidator<SupportRequest>
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

    public SupportRequestValidator()
    {
        RuleFor(x => x.AllowProjectSupport)
            .NotNull()
            .WithMessage("AllowProjectSupport should be true or false");
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message can't be empty")
            .MinimumLength(5)
            .WithMessage("Message length should contain atleast 5 characters")
            .MaximumLength(5000)
            .WithMessage("Message length should contain no more than 5000 characters");

        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Subject can't be empty")
            .MaximumLength(200)
            .WithMessage("Subject length should contain no more than 200 characters");

        RuleFor(x => x.Screenshots)
            .Must(files => (files?.Count ?? 0) <= 5)
            .WithMessage("A maximum of 5 screenshots is allowed");

        RuleForEach(x => x.Screenshots)
            .ChildRules(file =>
            {
                file.RuleFor(f => f.Length)
                    .LessThanOrEqualTo(5 * 1024 * 1024)
                    .WithMessage("Each file must be 5MB or less");

                file.RuleFor(f => f.ContentType)
                    .Must(x => AllowedMimeTypes.Contains(x))
                    .WithMessage("File type is not allowed");
            });
    }
}
