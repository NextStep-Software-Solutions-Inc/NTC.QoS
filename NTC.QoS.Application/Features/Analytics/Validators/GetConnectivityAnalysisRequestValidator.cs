using FluentValidation;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.Analytics.Validators;

public class GetConnectivityAnalysisRequestValidator : AbstractValidator<GetConnectivityAnalysisRequest>
{
    private readonly IBarangayRepository _barangayRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IRepository<NTC.QoS.Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<NTC.QoS.Domain.Entities.Region> _regionRepository;

    public GetConnectivityAnalysisRequestValidator(
        IBarangayRepository barangayRepository,
        IMunicipalityRepository municipalityRepository,
        IRepository<NTC.QoS.Domain.Entities.Province> provinceRepository,
        IRepository<NTC.QoS.Domain.Entities.Region> regionRepository)
    {
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;

        RuleFor(x => x.LocationIds)
            .NotNull()
            .WithMessage("LocationIds is required.")
            .NotEmpty()
            .WithMessage("At least one LocationId must be provided.")
            .Must(ids => ids.Count <= 100)
            .WithMessage("Cannot process more than 100 locations at once for performance reasons.")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("All LocationIds must be valid (non-empty) GUIDs.");

        RuleFor(x => x.LocationType)
            .IsInEnum()
            .WithMessage("LocationType must be a valid enum value (Barangay, Municipality, Provincial, or Regional).");

        RuleFor(x => x.FilterByStatus)
            .IsInEnum()
            .When(x => x.FilterByStatus.HasValue)
            .WithMessage("FilterByStatus must be a valid ConnectivityStatusEnum value (Served, Underserved, or Unserved) when provided.");

        // Conditional validation for meaningful combinations
        RuleFor(x => x)
            .Must(x => x.IncludeSpeedTestData || x.IncludeInfrastructureData)
            .WithMessage("At least one of IncludeSpeedTestData or IncludeInfrastructureData must be true to return meaningful data.");

        // Validate location IDs exist and match the specified location type
        RuleFor(x => x)
            .MustAsync(async (request, cancellationToken) =>
                {
                    var validationResult = await ValidateLocationIdsAsync(request.LocationIds, request.LocationType, cancellationToken);
                    return validationResult.IsValid;
                })
            .WithMessage(x => $"Location validation failed. {GetValidationErrorMessage(x.LocationIds, x.LocationType).Result}");
    }

    private async Task<LocationValidationResult> ValidateLocationIdsAsync(
        List<Guid> locationIds,
        LocationTypeEnum locationType,
        CancellationToken cancellationToken)
    {
        var result = new LocationValidationResult { IsValid = true };

        try
        {
            var existingIds = locationType switch
            {
                LocationTypeEnum.Barangay => await GetBarangayIdsAsync(locationIds, cancellationToken),
                LocationTypeEnum.Municipality => await GetMunicipalityIdsAsync(locationIds, cancellationToken),
                LocationTypeEnum.Provincial => await GetProvinceIdsAsync(locationIds, cancellationToken),
                LocationTypeEnum.Regional => await GetRegionIdsAsync(locationIds, cancellationToken),
                _ => new List<Guid>()
            };

            var missingIds = locationIds.Except(existingIds).ToList();

            if (missingIds.Any())
            {
                result.IsValid = false;
                result.MissingIds = missingIds;
            }
        }
        catch (Exception)
        {
            result.IsValid = false;
            result.ErrorMessage = "An error occurred while validating location IDs.";
        }

        return result;
    }

    private async Task<string> GetValidationErrorMessage(List<Guid> locationIds, LocationTypeEnum locationType)
    {
        var validationResult = await ValidateLocationIdsAsync(locationIds, locationType, CancellationToken.None);

        if (!validationResult.IsValid)
        {
            if (validationResult.MissingIds?.Any() == true)
            {
                var locationTypeName = locationType switch
                {
                    LocationTypeEnum.Barangay => "Barangay",
                    LocationTypeEnum.Municipality => "Municipality",
                    LocationTypeEnum.Provincial => "Province",
                    LocationTypeEnum.Regional => "Region",
                    _ => "Location"
                };

                var missingIdsText = string.Join(", ", validationResult.MissingIds.Take(5));
                var additionalCount = validationResult.MissingIds.Count > 5
                    ? $" and {validationResult.MissingIds.Count - 5} more"
                    : "";

                return $"The following {locationTypeName} ID(s) were not found: {missingIdsText}{additionalCount}. " +
                       $"Please verify that the IDs exist and match the specified LocationType ({locationType}).";
            }

            return validationResult.ErrorMessage ?? "Location validation failed.";
        }

        return string.Empty;
    }

    private async Task<List<Guid>> GetBarangayIdsAsync(List<Guid> locationIds, CancellationToken cancellationToken)
    {
        var query = _barangayRepository.Query(b => locationIds.Contains(b.Id))
            .Select(b => b.Id);
        return await _barangayRepository.ToListAsync(query, cancellationToken);
    }

    private async Task<List<Guid>> GetMunicipalityIdsAsync(List<Guid> locationIds, CancellationToken cancellationToken)
    {
        var query = _municipalityRepository.Query(m => locationIds.Contains(m.Id))
            .Select(m => m.Id);
        return await _municipalityRepository.ToListAsync(query, cancellationToken);
    }

    private async Task<List<Guid>> GetProvinceIdsAsync(List<Guid> locationIds, CancellationToken cancellationToken)
    {
        var query = _provinceRepository.Query(p => locationIds.Contains(p.Id))
            .Select(p => p.Id);
        return await _provinceRepository.ToListAsync(query, cancellationToken);
    }

    private async Task<List<Guid>> GetRegionIdsAsync(List<Guid> locationIds, CancellationToken cancellationToken)
    {
        var query = _regionRepository.Query(r => locationIds.Contains(r.Id))
            .Select(r => r.Id);
        return await _regionRepository.ToListAsync(query, cancellationToken);
    }

    private class LocationValidationResult
    {
        public bool IsValid { get; set; }
        public List<Guid>? MissingIds { get; set; }
        public string? ErrorMessage { get; set; }
    }
}