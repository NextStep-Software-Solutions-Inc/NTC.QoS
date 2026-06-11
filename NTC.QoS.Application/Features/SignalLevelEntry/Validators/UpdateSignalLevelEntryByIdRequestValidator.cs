using FluentValidation;
using NTC.QoS.Application.Features.SignalLevelEntry.Requests;

namespace NTC.QoS.Application.Features.SignalLevelEntry.Validators;

public class UpdateSignalLevelEntryByIdRequestValidator : AbstractValidator<UpdateSignalLevelEntryByIdRequest>
{
    public UpdateSignalLevelEntryByIdRequestValidator()
    {
        When(x => x.SignalLevelEntry?.Color != null, () =>
        {
            RuleFor(x => x.SignalLevelEntry.Color!)
                .Matches(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
                .WithMessage("Color must be a valid hex color code (e.g. #RRGGBB or #RGB).");
        });

        When(x => x.SignalLevelEntry?.SortOrder != null, () =>
        {
            RuleFor(x => x.SignalLevelEntry.SortOrder!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("SortOrder must be a non-negative integer.");
        });
    }
}
