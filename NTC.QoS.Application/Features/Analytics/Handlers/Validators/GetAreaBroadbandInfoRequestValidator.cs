using FluentValidation;
using NTC.QoS.Application.Features.Analytics.Requests;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Validators;

public class GetAreaBroadbandInfoRequestValidator : AbstractValidator<GetAreaBroadbandInfoRequest>
{
    public GetAreaBroadbandInfoRequestValidator()
    {
        RuleFor(x => x.BoundingBox)
            .NotNull()
            .WithMessage("Bounding box is required.")
            .SetValidator(new BoundingBoxDtoValidator());
    }
}