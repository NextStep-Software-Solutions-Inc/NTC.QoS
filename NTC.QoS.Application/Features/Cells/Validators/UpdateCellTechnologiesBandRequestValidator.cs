using FluentValidation;
using NTC.QoS.Application.Features.Cells.Requests;

namespace NTC.QoS.Application.Features.Cells.Validators;

public class UpdateCellTechnologiesBandRequestValidator : AbstractValidator<UpdateCellTechnologiesBandRequest>
{
    public UpdateCellTechnologiesBandRequestValidator()
    {
        RuleFor(x => x.CellId)
            .NotEmpty().WithMessage("CellId is required.");

        RuleFor(x => x.Payload)
            .NotNull().WithMessage("Payload is required.");

        When(x => x.Payload != null, () =>
        {
            RuleFor(x => x.Payload.TechnologyBands)
                .NotNull().WithMessage("TechnologyBands list is required.");

            When(x => x.Payload.TechnologyBands != null, () =>
            {
                RuleForEach(x => x.Payload.TechnologyBands).ChildRules(item =>
                {
                    item.RuleFor(x => x.TechnologyId)
                        .NotEmpty().WithMessage("TechnologyId is required.");

                    item.RuleFor(x => x.BandId)
                        .NotEmpty().WithMessage("BandId is required.");
                });
            });
        });
    }
}
