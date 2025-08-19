using FluentValidation;
using NTC.QoS.Application.Features.Provider.Requests;

namespace NTC.QoS.Application.Features.Provider.Handlers.Validators;

public class AddProviderLogoByIdRequestValidator : AbstractValidator<AddProviderLogoByIdRequest>
{
    private readonly long _maxFileSizeInBytes = 5 * 1024 * 1024; // 5MB
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private readonly string[] _allowedContentTypes = { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };

    public AddProviderLogoByIdRequestValidator()
    {
        // Validate Provider ID
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Provider ID is required.");

        // Validate Logo DTO
        RuleFor(x => x.Logo)
            .NotNull()
            .WithMessage("Logo information is required.");

        // Validate Logo File when Logo DTO is not null
        When(x => x.Logo != null, () =>
        {
            RuleFor(x => x.Logo.LogoFile)
                .NotNull()
                .WithMessage("Logo file is required.");

            // Validate file when LogoFile is not null
            When(x => x.Logo.LogoFile != null, () =>
            {
                RuleFor(x => x.Logo.LogoFile.Length)
                    .GreaterThan(0)
                    .WithMessage("Logo file cannot be empty.")
                    .LessThanOrEqualTo(_maxFileSizeInBytes)
                    .WithMessage($"Logo file size must not exceed {_maxFileSizeInBytes / (1024 * 1024)} MB.");

                RuleFor(x => x.Logo.LogoFile.FileName)
                    .NotEmpty()
                    .WithMessage("Logo file name is required.")
                    .Must(HaveValidExtension)
                    .WithMessage($"Only image files are allowed. Supported formats: {string.Join(", ", _allowedExtensions)}");

                RuleFor(x => x.Logo.LogoFile.ContentType)
                    .NotEmpty()
                    .WithMessage("Logo file content type is required.")
                    .Must(BeValidContentType)
                    .WithMessage($"Invalid file type. Only image files are allowed: {string.Join(", ", _allowedContentTypes)}");
            });
        });
    }

    private bool HaveValidExtension(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return _allowedExtensions.Contains(extension);
    }

    private bool BeValidContentType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        return _allowedContentTypes.Contains(contentType.ToLowerInvariant());
    }
}