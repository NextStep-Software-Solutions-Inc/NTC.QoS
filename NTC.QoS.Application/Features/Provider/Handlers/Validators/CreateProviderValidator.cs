using FluentValidation;
using NTC.QoS.Application.Features.Provider.Requests;

namespace NTC.QoS.Application.Features.Provider.Handlers.Validators;

public class CreateProviderValidator : AbstractValidator<CreateProviderRequest>
{
    public CreateProviderValidator()
    {
        RuleFor(x => x.Provider.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");
        RuleFor(x => x.Provider.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
        RuleFor(x => x.Provider.LicenseNumber)
            .MaximumLength(50).WithMessage("License number must not exceed 50 characters");
    }
}
