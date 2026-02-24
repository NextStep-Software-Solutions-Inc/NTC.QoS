using FluentValidation;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.NAP.Validators;

public class UpdateNAPRequestValidator : AbstractValidator<UpdateNAPRequest>
{
    private readonly IServiceProviderRepository _serviceProviderRepository;
    private readonly INAPRepository _napRepository;

    public UpdateNAPRequestValidator(IServiceProviderRepository serviceProviderRepository, INAPRepository napRepository)
    {
        _serviceProviderRepository = serviceProviderRepository;
        _napRepository = napRepository;

        RuleFor(x => x.NAP)
            .NotNull()
            .WithMessage("NAP payload is required.");

        When(x => x.NAP != null, () =>
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("NAP Id is required.")
                .MustAsync(async (napId, cancellationToken) =>
                {
                    var query = _napRepository.QueryAsNoTracking(n => n.Id == napId);
                    var nap = await _napRepository.FirstOrDefaultAsync(query, cancellationToken);
                    return nap != null;
                })
                .WithMessage("The specified NAP does not exist.");

            // Validate Latitude only if provided
            When(x => x.NAP.Latitude.HasValue, () =>
            {
                RuleFor(x => x.NAP.Latitude!.Value)
                    .InclusiveBetween(-90d, 90d)
                    .WithMessage("Latitude must be between -90 and 90.");
            });

            // Validate Longitude only if provided
            When(x => x.NAP.Longitude.HasValue, () =>
            {
                RuleFor(x => x.NAP.Longitude!.Value)
                    .InclusiveBetween(-180d, 180d)
                    .WithMessage("Longitude must be between -180 and 180.");
            });

            // When either coordinate is provided, both must be provided
            RuleFor(x => x.NAP)
                .Must(nap => (nap.Latitude.HasValue && nap.Longitude.HasValue) || (!nap.Latitude.HasValue && !nap.Longitude.HasValue))
                .WithMessage("Both Latitude and Longitude must be provided together for location updates.");

            // Validate ServiceProviderId only if provided
            When(x => x.NAP.ServiceProviderId.HasValue, () =>
            {
                RuleFor(x => x.NAP.ServiceProviderId!.Value)
                    .NotEmpty()
                    .WithMessage("ServiceProviderId cannot be empty.")
                    .MustAsync(async (serviceProviderId, cancellationToken) =>
                    {
                        var query = _serviceProviderRepository.QueryAsNoTracking(sp => sp.Id == serviceProviderId);
                        var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(query, cancellationToken);
                        return serviceProvider != null;
                    })
                    .WithMessage("The specified Service Provider does not exist.");
            });

            // Validate Address only if provided
            When(x => !string.IsNullOrEmpty(x.NAP.Address), () =>
            {
                RuleFor(x => x.NAP.Address)
                    .MaximumLength(500)
                    .WithMessage("Address must not exceed 500 characters.");
            });
        });
    }
}
