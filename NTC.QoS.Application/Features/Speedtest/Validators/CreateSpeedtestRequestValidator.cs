using FluentValidation;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.Speedtest.Validators;

public class CreateSpeedtestRequestValidator : AbstractValidator<CreateSpeedtestRequest>
{
    private readonly IServiceProviderRepository _serviceProviderRepository;
    private readonly IGenerationRepository _generationRepository;
    private readonly ITechnologyRepository _technologyRepository;

    public CreateSpeedtestRequestValidator(
        IServiceProviderRepository serviceProviderRepository,
        IGenerationRepository generationRepository,
        ITechnologyRepository technologyRepository)
    {
        _serviceProviderRepository = serviceProviderRepository;
        _generationRepository = generationRepository;
        _technologyRepository = technologyRepository;

        RuleFor(x => x.Speedtest)
            .NotNull()
            .WithMessage("Speedtest payload is required.");

        When(x => x.Speedtest != null, () =>
        {
            // Coordinate validations
            RuleFor(x => x.Speedtest.Latitude)
                .InclusiveBetween(-90d, 90d)
                .WithMessage("Latitude must be between -90 and 90.");

            RuleFor(x => x.Speedtest.Longitude)
                .InclusiveBetween(-180d, 180d)
                .WithMessage("Longitude must be between -180 and 180.");

            // Null Island validation (0,0)
            RuleFor(x => x.Speedtest)
                .Must(speedtest =>
                {
                    const double tolerance = 0.000001; // ~11cm precision
                    return !(Math.Abs(speedtest.Latitude) < tolerance && Math.Abs(speedtest.Longitude) < tolerance);
                })
                .WithMessage("Latitude and Longitude cannot both be 0 (invalid location).");

            // Speed validations
            RuleFor(x => x.Speedtest.UploadSpeed)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Upload speed cannot be negative.");

            RuleFor(x => x.Speedtest.DownloadSpeed)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Download speed cannot be negative.");

            // At least one speed must be > 0 (unless No Broadband Coverage)
            RuleFor(x => x.Speedtest)
                .Must(speedtest => speedtest.IsNoBroadbandCoverage || speedtest.UploadSpeed > 0 || speedtest.DownloadSpeed > 0)
                .WithMessage("At least one of Upload or Download speed must be greater than 0.");

            // Ping and Jitter validations (optional)
            When(x => x.Speedtest.Ping.HasValue, () =>
            {
                RuleFor(x => x.Speedtest.Ping!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Ping cannot be negative.");
            });

            When(x => x.Speedtest.Jitter.HasValue, () =>
            {
                RuleFor(x => x.Speedtest.Jitter!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Jitter cannot be negative.");
            });

            // Service Provider validation (required)
            RuleFor(x => x.Speedtest.ServiceProviderId)
                .NotEmpty()
                .WithMessage("ServiceProviderId is required.")
                .MustAsync(async (serviceProviderId, cancellationToken) =>
                {
                    var query = _serviceProviderRepository.QueryAsNoTracking(sp => sp.Id == serviceProviderId);
                    var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(query, cancellationToken);
                    return serviceProvider != null;
                })
                .WithMessage("The specified Service Provider does not exist.");

            // Generation validation (required)
            RuleFor(x => x.Speedtest.GenerationId)
                .NotEmpty()
                .WithMessage("GenerationId is required.")
                .MustAsync(async (generationId, cancellationToken) =>
                {
                    var query = _generationRepository.QueryAsNoTracking(g => g.Id == generationId);
                    var generation = await _generationRepository.FirstOrDefaultAsync(query, cancellationToken);
                    return generation != null;
                })
                .WithMessage("The specified Generation does not exist.");

            // Technology validation (required)
            RuleFor(x => x.Speedtest.TechnologyId)
                .NotEmpty()
                .WithMessage("TechnologyId is required.")
                .MustAsync(async (technologyId, cancellationToken) =>
                {
                    var query = _technologyRepository.QueryAsNoTracking(t => t.Id == technologyId);
                    var technology = await _technologyRepository.FirstOrDefaultAsync(query, cancellationToken);
                    return technology != null;
                })
                .WithMessage("The specified Technology does not exist.");

            // Address validation
            RuleFor(x => x.Speedtest.Address)
                .MaximumLength(500)
                .WithMessage("Address must not exceed 500 characters.");

            // Optional string fields (only validate if provided)
            When(x => !string.IsNullOrEmpty(x.Speedtest.PhoneModel), () =>
            {
                RuleFor(x => x.Speedtest.PhoneModel)
                    .MaximumLength(200)
                    .WithMessage("PhoneModel must not exceed 200 characters.");
            });

            When(x => !string.IsNullOrEmpty(x.Speedtest.MCC), () =>
            {
                RuleFor(x => x.Speedtest.MCC)
                    .MaximumLength(10)
                    .WithMessage("MCC must not exceed 10 characters.");
            });

            When(x => !string.IsNullOrEmpty(x.Speedtest.MNC), () =>
            {
                RuleFor(x => x.Speedtest.MNC)
                    .MaximumLength(10)
                    .WithMessage("MNC must not exceed 10 characters.");
            });

            When(x => !string.IsNullOrEmpty(x.Speedtest.TAC), () =>
            {
                RuleFor(x => x.Speedtest.TAC)
                    .MaximumLength(20)
                    .WithMessage("TAC must not exceed 20 characters.");
            });

            When(x => !string.IsNullOrEmpty(x.Speedtest.CellIdentifier), () =>
            {
                RuleFor(x => x.Speedtest.CellIdentifier)
                    .MaximumLength(100)
                    .WithMessage("CellIdentifier must not exceed 100 characters.");
            });

            // SignalQuality validation (optional)
            When(x => !string.IsNullOrWhiteSpace(x.Speedtest.SignalQuality), () =>
            {
                RuleFor(x => x.Speedtest.SignalQuality!)
                    .Must(signalQuality =>
                    {
                        try
                        {
                            EnumHelper<SignalQualityEnum>.ParseUserInput(signalQuality);
                            return true;
                        }
                        catch (ArgumentException)
                        {
                            return false;
                        }
                    })
                    .WithMessage(speedtest =>
                    {
                        var validValues = string.Join(", ", Enum.GetNames(typeof(SignalQualityEnum)));
                        return $"Invalid SignalQuality value '{speedtest.Speedtest.SignalQuality}'. Valid values are: {validValues}";
                    });
            });
        });
    }
}
