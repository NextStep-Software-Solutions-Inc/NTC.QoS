using FluentValidation;
using NTC.QoS.Application.Features.File.Requests;

namespace NTC.QoS.Application.Features.File.Handlers.Validators;

public class DownloadWirelessValidationTemplateRequestValidator : AbstractValidator<DownloadWirelessValidationTemplateRequest>
{
    public DownloadWirelessValidationTemplateRequestValidator()
    {
        RuleFor(x => x.MunicipalityId)
            .NotEmpty()
            .WithMessage("Municipality ID is required.")
            .NotEqual(Guid.Empty)
            .WithMessage("Municipality ID must be a valid GUID.");
    }
}