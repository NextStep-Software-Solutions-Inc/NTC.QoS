using FluentValidation;
using NTC.QoS.Application.Features.User.Requests;

namespace NTC.QoS.Application.Features.User.Handlers.Validators;

public class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserRequestValidator()
    {
        RuleFor(x => x.User.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.User.RoleId)
            .NotEmpty().WithMessage("RoleId is required.");

        RuleForEach(x => x.User.Regions)
            .ChildRules(region =>
            {
                region.RuleFor(r => r.RegionId)
                    .NotEmpty().WithMessage("RegionId is required for each region assignment.");
            });
    }
}