using FluentValidation;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.NAP.Validators;

public class CreateNAPRequestValidator : AbstractValidator<CreateNAPRequest>
{
    private readonly IServiceProviderRepository _serviceProviderRepository;

    public CreateNAPRequestValidator(IServiceProviderRepository serviceProviderRepository)
    {
        _serviceProviderRepository = serviceProviderRepository;

        RuleFor(x => x.NAP)
            .NotNull()
            .WithMessage("NAP payload is required.");

        When(x => x.NAP != null, () =>
        {
            RuleFor(x => x.NAP.Latitude)
                .InclusiveBetween(-90d, 90d)
                .WithMessage("Latitude must be between -90 and 90.");

            RuleFor(x => x.NAP.Longitude)
                .InclusiveBetween(-180d, 180d)
                .WithMessage("Longitude must be between -180 and 180.");

            RuleFor(x => x.NAP.ServiceProviderId)
                .NotEmpty()
                .WithMessage("ServiceProviderId is required.")
                .MustAsync(async (serviceProviderId, cancellationToken) =>
                {
                    var query = _serviceProviderRepository.QueryAsNoTracking(sp =>
                        sp.Id == serviceProviderId &&
                        sp.Service != null &&
                        sp.Service.Type == ServiceTypeEnum.Wired);
                    var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(query, cancellationToken);
                    return serviceProvider != null;
                })
                .WithMessage("The specified Service Provider does not exist or is not wired.");

            RuleFor(x => x.NAP.Address)
                .MaximumLength(500)
                .WithMessage("Address must not exceed 500 characters.");
        });
    }
}
