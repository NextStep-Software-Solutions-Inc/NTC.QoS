using FluentValidation;
using NTC.QoS.Application.Features.SignalLevelEntry.Requests;

namespace NTC.QoS.Application.Features.SignalLevelEntry.Validators;

public class CreateSignalLevelEntryRequestValidator : AbstractValidator<CreateSignalLevelEntryRequest>
{
    public CreateSignalLevelEntryRequestValidator()
    {
        RuleFor(x => x.SignalLevelEntry)
            .NotNull()
            .WithMessage("Signal level entry payload is required.");

        When(x => x.SignalLevelEntry != null, () =>
        {
            RuleFor(x => x.SignalLevelEntry.Color)
                .NotEmpty()
                .WithMessage("Color is required.")
                .Matches(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
                .WithMessage("Color must be a valid hex color code (e.g. #RRGGBB or #RGB).");

            RuleFor(x => x.SignalLevelEntry.Description)
                .NotEmpty()
                .WithMessage("Description is required.");

            RuleFor(x => x.SignalLevelEntry.SortOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("SortOrder must be a non-negative integer.");
        });
    }
}
