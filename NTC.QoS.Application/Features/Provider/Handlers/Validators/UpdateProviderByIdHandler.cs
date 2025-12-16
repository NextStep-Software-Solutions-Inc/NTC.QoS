using FluentValidation;
using NTC.QoS.Application.Features.Provider.Requests;

namespace NTC.QoS.Application.Features.Provider.Handlers.Validators;

public class UpdateProviderByIdHandler : AbstractValidator<UpdateProviderByIdRequest>
{
    public UpdateProviderByIdHandler()
    {
        RuleFor(x => x.Provider.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

    }
}
