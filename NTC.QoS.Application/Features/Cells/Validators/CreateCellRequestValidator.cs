using System;
using FluentValidation;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Features.Cells.Validators
{
    public class CreateCellRequestValidator : AbstractValidator<CreateCellRequest>
    {
        public CreateCellRequestValidator()
        {
            RuleFor(x => x.Cell).NotNull().WithMessage("Cell payload is required.");

            When(x => x.Cell != null, () =>
            {
                RuleFor(x => x.Cell.Name)
                    .NotEmpty().WithMessage("Name is required.")
                    .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

                RuleFor(x => x.Cell.Latitude)
                    .InclusiveBetween(-90d, 90d).WithMessage("Latitude must be between -90 and 90.");

                RuleFor(x => x.Cell.Longitude)
                    .InclusiveBetween(-180d, 180d).WithMessage("Longitude must be between -180 and 180.");

                RuleFor(x => x.Cell.ServiceProviderId)
                    .NotEmpty().WithMessage("ServiceProviderId is required.");

                RuleFor(x => x.Cell.Status)
                    .NotEmpty().WithMessage("Status is required.")
                    .Must(s => Enum.TryParse<CellStatusEnum>(s, true, out _))
                    .WithMessage("Status is not a valid CellStatusEnum value.");

                RuleFor(x => x.Cell.TowerType)
                    .Must(t => string.IsNullOrEmpty(t) || Enum.TryParse<TowerTypeEnum>(t, true, out _))
                    .WithMessage("TowerType is not a valid TowerTypeEnum value.");
            });
        }
    }
}
