using FluentValidation;
using NTC.QoS.Application.Features.Region.Requests;

namespace NTC.QoS.Application.Features.Region.Handlers.Validator;

public class CreateRegionValidator : AbstractValidator<CreateRegionRequest>
{
    public CreateRegionValidator()
    {
        RuleFor(x => x.Region.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
    }
}
