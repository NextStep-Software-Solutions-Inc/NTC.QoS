using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NetTopologySuite.Geometries;

namespace NTC.QoS.Application.Features.Band.Queries.GetTechnologyBandsByLocation;

public class GetTechnologyBandsByLocationHandler : IRequestHandler<GetTechnologyBandsByLocationQuery, DetailedTechnologyBandBreakdownDto>
{
    private readonly IBarangayRepository _barangayRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly IRegionRepository _regionRepository;
    private readonly ICellularRepository _cellularRepository;
    private readonly IWirelessValidationRepository _wirelessValidationRepository;
    private readonly ILogger<GetTechnologyBandsByLocationHandler> _logger;

    public GetTechnologyBandsByLocationHandler(
        IBarangayRepository barangayRepository,
        IMunicipalityRepository municipalityRepository,
        IProvinceRepository provinceRepository,
        IRegionRepository regionRepository,
        ICellularRepository cellularRepository,
        IWirelessValidationRepository wirelessValidationRepository,
        ILogger<GetTechnologyBandsByLocationHandler> logger)
    {
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _cellularRepository = cellularRepository;
        _wirelessValidationRepository = wirelessValidationRepository;
        _logger = logger;
    }

    public async Task<DetailedTechnologyBandBreakdownDto> Handle(
        GetTechnologyBandsByLocationQuery request,
        CancellationToken cancellationToken)
    {
        // Get the boundary geometry for the location
        Geometry? boundary = null;
        var locationTypeLower = request.LocationType.ToLower();

        try
        {
            if (locationTypeLower == "barangay")
            {
                var brgyQuery = _barangayRepository.Query(b => b.Id == Guid.Parse(request.LocationId));
                var brgy = await _barangayRepository.FirstOrDefaultAsync(brgyQuery, cancellationToken);
                boundary = brgy?.Boundaries;
            }
            else if (locationTypeLower == "municipality")
            {
                var munQuery = _municipalityRepository.Query(m => m.Id == Guid.Parse(request.LocationId));
                var mun = await _municipalityRepository.FirstOrDefaultAsync(munQuery, cancellationToken);
                boundary = mun?.Boundaries;
            }
            else if (locationTypeLower == "province")
            {
                var provQuery = _provinceRepository.Query(p => p.Id == Guid.Parse(request.LocationId));
                var prov = await _provinceRepository.FirstOrDefaultAsync(provQuery, cancellationToken);
                boundary = prov?.Boundaries;
            }
            else if (locationTypeLower == "region")
            {
                var regQuery = _regionRepository.Query(r => r.Id == Guid.Parse(request.LocationId));
                var reg = await _regionRepository.FirstOrDefaultAsync(regQuery, cancellationToken);
                boundary = reg?.Boundaries;
            }

            if (boundary == null)
            {
                _logger.LogWarning("Location not found: {LocationId} ({LocationType})", request.LocationId, request.LocationType);
                return new DetailedTechnologyBandBreakdownDto();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving location boundary for {LocationId} ({LocationType})", request.LocationId, request.LocationType);
            return new DetailedTechnologyBandBreakdownDto();
        }

        // THREE HashSets per technology
        var cellularBands = new Dictionary<string, HashSet<string>>();
        var validationBands = new Dictionary<string, HashSet<string>>();
        var allBands = new Dictionary<string, HashSet<string>>();
        
        // Track unique bands per generation and technology occurrences
        var generationBands = new Dictionary<string, HashSet<string>>(); // Track unique bands per generation
        var generationSet = new HashSet<string>();
        var technologySet = new HashSet<string>(); // Track unique technologies encountered

        // Query 1: Get bands from cellular infrastructure
        try
        {
            var cellularQuery = _cellularRepository.Query()
                .Where(c => c.CellSector != null 
                         && c.CellSector.Cell != null 
                         && c.CellSector.Cell.Location != null
                         && boundary.Contains(c.CellSector.Cell.Location)
                         && c.Band != null 
                         && c.Technology != null 
                         && c.Technology.Standard != null
                         && c.Technology.Generation != null);

            var cellularTechBandData = await _cellularRepository.ToListAsync(
                cellularQuery.Select(c => new
                {
                    TechnologyStandard = c.Technology!.Standard!,
                    BandName = c.Band!.Name!,
                    GenerationName = c.Technology.Generation!.Name!
                }).Distinct(), 
                cancellationToken);

            // Process cellular infrastructure bands
            foreach (var item in cellularTechBandData)
            {
                if (!cellularBands.ContainsKey(item.TechnologyStandard))
                {
                    cellularBands[item.TechnologyStandard] = new HashSet<string>();
                    allBands[item.TechnologyStandard] = new HashSet<string>();
                }
                cellularBands[item.TechnologyStandard].Add(item.BandName);
                allBands[item.TechnologyStandard].Add(item.BandName);
                
                // Track unique bands per generation
                if (!generationBands.ContainsKey(item.GenerationName))
                {
                    generationBands[item.GenerationName] = new HashSet<string>();
                }
                generationBands[item.GenerationName].Add(item.BandName);
                
                // Track unique technologies and generations
                technologySet.Add(item.TechnologyStandard);
                generationSet.Add(item.GenerationName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cellular bands for location {LocationId}", request.LocationId);
        }

        // Query 2: Get bands from wireless validation measurements
        try
        {
            var validationQuery = _wirelessValidationRepository.Query()
                .Where(v => v.Barangay != null 
                         && v.Barangay.Center != null
                         && boundary.Contains(v.Barangay.Center)
                         && v.Band != null 
                         && v.Technology != null 
                         && v.Technology.Standard != null
                         && v.Technology.Generation != null);

            var validationTechBandData = await _wirelessValidationRepository.ToListAsync(
                validationQuery.Select(v => new
                {
                    TechnologyStandard = v.Technology!.Standard!,
                    BandName = v.Band!.Name!,
                    GenerationName = v.Technology.Generation!.Name!
                }).Distinct(),
                cancellationToken);

            // Process validation measurement bands
            foreach (var item in validationTechBandData)
            {
                if (!validationBands.ContainsKey(item.TechnologyStandard))
                {
                    validationBands[item.TechnologyStandard] = new HashSet<string>();
                }
                if (!allBands.ContainsKey(item.TechnologyStandard))
                {
                    allBands[item.TechnologyStandard] = new HashSet<string>();
                }
                validationBands[item.TechnologyStandard].Add(item.BandName);
                allBands[item.TechnologyStandard].Add(item.BandName);
                
                // Track unique bands per generation
                if (!generationBands.ContainsKey(item.GenerationName))
                {
                    generationBands[item.GenerationName] = new HashSet<string>();
                }
                generationBands[item.GenerationName].Add(item.BandName);
                
                // Track unique technologies and generations
                technologySet.Add(item.TechnologyStandard);
                generationSet.Add(item.GenerationName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving validation bands for location {LocationId}", request.LocationId);
        }

        // Build the result DTO
        var result = new DetailedTechnologyBandBreakdownDto();

        foreach (var tech in allBands.Keys.OrderBy(k => k))
        {
            result[tech] = new TechnologyBandDetailDto
            {
                CellularBands = cellularBands.ContainsKey(tech)
                    ? cellularBands[tech].OrderBy(b => b).ToList()
                    : [],
                ValidationBands = validationBands.ContainsKey(tech)
                    ? validationBands[tech].OrderBy(b => b).ToList()
                    : [],
                AllBands = allBands[tech].OrderBy(b => b).ToList()
            };
        }

        // Populate summary properties
        result.AvailableTechnologies = allBands.Keys.OrderBy(t => t).ToList();
        result.AvailableGenerations = generationSet.OrderBy(g => g).ToList();
        result.AllBands = allBands.Values.SelectMany(v => v).Distinct().OrderBy(b => b).ToList();

        // Calculate generation counts (count of unique bands per generation)
        result.Count2G = generationBands.GetValueOrDefault("2G", new HashSet<string>()).Count;
        result.Count3G = generationBands.GetValueOrDefault("3G", new HashSet<string>()).Count;
        result.Count4G = generationBands.GetValueOrDefault("4G", new HashSet<string>()).Count;
        result.Count5G = generationBands.GetValueOrDefault("5G", new HashSet<string>()).Count;

        // Determine dominant generation (based on number of unique bands)
        if (generationBands.Any())
        {
            result.DominantGeneration = generationBands.OrderByDescending(kvp => kvp.Value.Count).First().Key;
        }

        // Determine primary technology (technology with most bands)
        if (allBands.Any())
        {
            result.PrimaryTechnology = allBands.OrderByDescending(kvp => kvp.Value.Count).First().Key;
        }

        // Calculate total technology count (number of unique technologies)
        result.TotalTechnologyCount = technologySet.Count;

        // Calculate modern coverage ratio (based on unique bands)
        var totalUniqueBands = result.AllBands.Count;
        var modernUniqueBands = result.Count4G + result.Count5G;
        result.HasModernCoverage = modernUniqueBands > 0;
        result.ModernCoverageRatio = totalUniqueBands > 0 
            ? (double)modernUniqueBands / totalUniqueBands 
            : 0;

        return result;
    }
}
