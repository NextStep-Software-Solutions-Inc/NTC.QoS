using FluentValidation;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.Speedtest.Validators;

public class UpdateSpeedtestRequestValidator : AbstractValidator<UpdateSpeedtestRequest>
{
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository;
    private readonly IGenerationRepository _generationRepository;
    private readonly ITechnologyRepository _technologyRepository;

    public UpdateSpeedtestRequestValidator(
        ISpeedtestRepository speedtestRepository,
        IServiceProviderRepository serviceProviderRepository,
        IGenerationRepository generationRepository,
        ITechnologyRepository technologyRepository)
    {
        _speedtestRepository = speedtestRepository;
        _serviceProviderRepository = serviceProviderRepository;
        _generationRepository = generationRepository;
        _technologyRepository = technologyRepository;

        RuleFor(x => x.Speedtest)
            .NotNull()
            .WithMessage("Speedtest payload is required.");

        When(x => x.Speedtest != null, () =>
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Speedtest Id is required.")
                .MustAsync(async (speedtestId, cancellationToken) =>
                {
                    var query = _speedtestRepository.QueryAsNoTracking(s => s.Id == speedtestId);
                    var speedtest = await _speedtestRepository.FirstOrDefaultAsync(query, cancellationToken);
                    return speedtest != null;
                })
                .WithMessage("The specified Speedtest does not exist.");

            // Validate Latitude only if provided
            When(x => x.Speedtest.Latitude.HasValue, () =>
            {
                RuleFor(x => x.Speedtest.Latitude!.Value)
                    .InclusiveBetween(-90d, 90d)
                    .WithMessage("Latitude must be between -90 and 90.");
            });

            // Validate Longitude only if provided
            When(x => x.Speedtest.Longitude.HasValue, () =>
            {
                RuleFor(x => x.Speedtest.Longitude!.Value)
                    .InclusiveBetween(-180d, 180d)
                    .WithMessage("Longitude must be between -180 and 180.");
            });

            // When either coordinate is provided, both must be provided
            RuleFor(x => x.Speedtest)
                .Must(speedtest => (speedtest.Latitude.HasValue && speedtest.Longitude.HasValue) || (!speedtest.Latitude.HasValue && !speedtest.Longitude.HasValue))
                .WithMessage("Both Latitude and Longitude must be provided together for location updates.");

            // Null Island validation (only if coordinates are being updated)
            When(x => x.Speedtest.Latitude.HasValue && x.Speedtest.Longitude.HasValue, () =>
            {
                RuleFor(x => x.Speedtest)
                    .Must(speedtest =>
                    {
                        const double tolerance = 0.000001; // ~11cm precision
                        return !(Math.Abs(speedtest.Latitude!.Value) < tolerance && Math.Abs(speedtest.Longitude!.Value) < tolerance);
                    })
                    .WithMessage("Latitude and Longitude cannot both be 0 (invalid location).");
            });

            // Speed validations (optional)
            When(x => x.Speedtest.UploadSpeed.HasValue, () =>
            {
                RuleFor(x => x.Speedtest.UploadSpeed!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Upload speed cannot be negative.");
            });

            When(x => x.Speedtest.DownloadSpeed.HasValue, () =>
            {
                RuleFor(x => x.Speedtest.DownloadSpeed!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Download speed cannot be negative.");
            });

            // If either speed is provided, ensure that at least one of the (possibly updated) speeds is > 0.
            RuleFor(x => x.Speedtest)
                .Must(speedtest =>
                {
                    var upload = speedtest.UploadSpeed ?? 0;
                    var download = speedtest.DownloadSpeed ?? 0;
                    return upload > 0 || download > 0;
                })
                .When(x => x.Speedtest.UploadSpeed.HasValue || x.Speedtest.DownloadSpeed.HasValue)
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

            // Service Provider validation (optional)
            When(x => x.Speedtest.ServiceProviderId.HasValue, () =>
            {
                RuleFor(x => x.Speedtest.ServiceProviderId!.Value)
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

            // Generation validation (optional)
            When(x => x.Speedtest.GenerationId.HasValue, () =>
            {
                RuleFor(x => x.Speedtest.GenerationId!.Value)
                    .MustAsync(async (generationId, cancellationToken) =>
                    {
                        var query = _generationRepository.QueryAsNoTracking(g => g.Id == generationId);
                        var generation = await _generationRepository.FirstOrDefaultAsync(query, cancellationToken);
                        return generation != null;
                    })
                    .WithMessage("The specified Generation does not exist.");
            });

            // Technology validation (optional)
            When(x => x.Speedtest.TechnologyId.HasValue, () =>
            {
                RuleFor(x => x.Speedtest.TechnologyId!.Value)
                    .MustAsync(async (technologyId, cancellationToken) =>
                    {
                        var query = _technologyRepository.QueryAsNoTracking(t => t.Id == technologyId);
                        var technology = await _technologyRepository.FirstOrDefaultAsync(query, cancellationToken);
                        return technology != null;
                    })
                    .WithMessage("The specified Technology does not exist.");
            });

            // Address validation (optional)
            When(x => !string.IsNullOrEmpty(x.Speedtest.Address), () =>
            {
                RuleFor(x => x.Speedtest.Address)
                    .MaximumLength(500)
                    .WithMessage("Address must not exceed 500 characters.");
            });

            // String field validations (optional)
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
