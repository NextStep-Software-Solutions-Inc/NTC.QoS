using FluentValidation;
using NTC.QoS.Application.Features.Cellular.Requests;

namespace NTC.QoS.Application.Features.Cellular.Validators;

public class CreateCellularRequestValidator : AbstractValidator<CreateCellularRequest>
{
    public CreateCellularRequestValidator()
    {
        RuleFor(x => x.Cellular)
            .NotNull().WithMessage("Cellular payload is required.");

        When(x => x.Cellular != null, () =>
        {
            RuleFor(x => x.Cellular.CellSectorId)
                .NotEmpty().WithMessage("CellSectorId is required.");

            RuleFor(x => x.Cellular.ServiceProviderId)
                .NotEmpty().WithMessage("ServiceProviderId is required.");

            RuleFor(x => x.Cellular.TechnologyId)
                .NotEmpty().WithMessage("TechnologyId is required.");

            RuleFor(x => x.Cellular.BandId)
                .NotEmpty().WithMessage("BandId is required.");
        });
    }
}
