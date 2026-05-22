using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

/// <summary>
/// Handler for updating a cell's supported technologies and bands.
/// Manages Cellular records independently from cellsite metadata updates.
/// </summary>
public class UpdateCellTechnologiesBandHandler(
    ICellRepository cellRepository,
    ICellSectorRepository cellSectorRepository,
    IRepository<Domain.Entities.Cellular> cellularRepository,
    ITechnologyRepository technologyRepository,
    IBandRepository bandRepository,
    IServiceProviderRepository serviceProviderRepository,
    INetworkRepository networkRepository,
    ISectorRepository sectorRepository,
    IMunicipalityRepository municipalityRepository,
    IRepository<Domain.Entities.SystemType> systemTypeRepository,
    ILogger<UpdateCellTechnologiesBandHandler> logger)
    : IRequestHandler<UpdateCellTechnologiesBandRequest, ApiResponse<List<CellTechnologyBandDto>>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly ICellSectorRepository _cellSectorRepository = cellSectorRepository;
    private readonly IRepository<Domain.Entities.Cellular> _cellularRepository = cellularRepository;
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;
    private readonly IBandRepository _bandRepository = bandRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly INetworkRepository _networkRepository = networkRepository;
    private readonly ISectorRepository _sectorRepository = sectorRepository;
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;
    private readonly IRepository<Domain.Entities.SystemType> _systemTypeRepository = systemTypeRepository;
    private readonly ILogger<UpdateCellTechnologiesBandHandler> _logger = logger;

    public async Task<ApiResponse<List<CellTechnologyBandDto>>> Handle(
        UpdateCellTechnologiesBandRequest request,
        CancellationToken cancellationToken)
    {
        // Verify cell exists
        var cellQuery = _cellRepository.Query(c => c.Id == request.CellId);
        var cell = await _cellRepository.FirstOrDefaultAsync(cellQuery, cancellationToken);

        if (cell is null)
        {
            return new ApiResponse<List<CellTechnologyBandDto>>
            {
                Message = "Cell not found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NullCell", ["Cell with the specified ID does not exist."] }
                }
            };
        }

        // Get all cell sectors for this cell
        var sectorsQuery = _cellSectorRepository.Query(cs => cs.CellId == request.CellId);
        var sectors = await _cellSectorRepository.ToListAsync(sectorsQuery, cancellationToken);

        if (sectors.Count == 0)
        {
            return new ApiResponse<List<CellTechnologyBandDto>>
            {
                Message = "No cell sectors found for this cell.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "NoSectors", ["This cell has no associated sectors. Please add sectors before assigning technologies."] }
                }
            };
        }

        // Validate all technologies and bands exist
        var technologyIds = request.Payload.TechnologyBands.Select(tb => tb.TechnologyId).Distinct().ToList();
        var bandIds = request.Payload.TechnologyBands.Select(tb => tb.BandId).Distinct().ToList();

        var validTechnologies = await _technologyRepository.ToListAsync(
            _technologyRepository.Query(t => technologyIds.Contains(t.Id)),
            cancellationToken);

        if (validTechnologies.Count != technologyIds.Count)
        {
            var invalidTechIds = technologyIds.Except(validTechnologies.Select(t => t.Id)).ToList();
            return new ApiResponse<List<CellTechnologyBandDto>>
            {
                Message = "One or more technologies do not exist.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "InvalidTechnologies", [$"Technologies not found: {string.Join(", ", invalidTechIds)}"] }
                }
            };
        }

        var validBands = await _bandRepository.ToListAsync(
            _bandRepository.Query(b => bandIds.Contains(b.Id)),
            cancellationToken);

        if (validBands.Count != bandIds.Count)
        {
            var invalidBandIds = bandIds.Except(validBands.Select(b => b.Id)).ToList();
            return new ApiResponse<List<CellTechnologyBandDto>>
            {
                Message = "One or more bands do not exist.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "InvalidBands", [$"Bands not found: {string.Join(", ", invalidBandIds)}"] }
                }
            };
        }

        // Validate all service provider IDs (cell default + any per-item overrides)
        var overrideProviderIds = request.Payload.TechnologyBands
            .Where(tb => tb.ServiceProviderId.HasValue)
            .Select(tb => tb.ServiceProviderId!.Value)
            .Distinct()
            .ToList();

        var allProviderIds = overrideProviderIds
            .Append(cell.ServiceProviderId)
            .Distinct()
            .ToList();

        var validProviderCount = await _serviceProviderRepository.CountAsync(
            _serviceProviderRepository.Query(sp => allProviderIds.Contains(sp.Id)),
            cancellationToken);

        if (validProviderCount != allProviderIds.Count)
        {
            return new ApiResponse<List<CellTechnologyBandDto>>
            {
                Message = "One or more service providers do not exist.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "InvalidServiceProviders", ["One or more ServiceProviderIds are invalid."] }
                }
            };
        }

        // Get existing Cellular records for this cell
        var existingCellularQuery = _cellularRepository.Query(c =>
            c.CellSector != null && c.CellSector.CellId == request.CellId);
        var existingCellulars = await _cellularRepository.ToListAsync(existingCellularQuery, cancellationToken);

        var defaultServiceProviderId = cell.ServiceProviderId;

        // Resolve municipality from cell location for network lookup/creation
        var (municipalityId, municipalityName) = await _municipalityRepository
            .GetMunicipalityIdAndNameByLocationAsync(cell.Location, cancellationToken);

        // Build lookup maps for technology standard and band name
        var techStandardById = validTechnologies.ToDictionary(t => t.Id, t => t.Standard);
        var bandNameById = validBands.ToDictionary(b => b.Id, b => b.Name);

        // Resolve network IDs for each unique (techId, bandId, providerId) combination
        var comboNetworkMap = new Dictionary<(Guid TechId, Guid BandId, Guid ProviderId), (Guid NetworkId, decimal DLFreq, decimal ULFreq, decimal Bandwidth)>();

        foreach (var techBand in request.Payload.TechnologyBands)
        {
            var providerId = techBand.ServiceProviderId ?? defaultServiceProviderId;
            var key = (techBand.TechnologyId, techBand.BandId, providerId);

            if (comboNetworkMap.ContainsKey(key))
                continue;

            var techStandard = techStandardById[techBand.TechnologyId];
            var bandName = bandNameById[techBand.BandId];

            var dlFreq = FrequencyHelper.GetDefaultDLCenterFreq(techStandard, bandName);
            var bandwidth = FrequencyHelper.GetDefaultBandwidth(techStandard);
            var ulFreq = FrequencyHelper.CalculateULCenterFreq(dlFreq, techStandard, bandName);

            var networkId = await GetOrCreateNetworkAsync(
                providerId,
                municipalityId,
                municipalityName ?? "Unknown",
                techBand.TechnologyId,
                techBand.BandId,
                techStandard,
                bandName,
                dlFreq,
                ulFreq,
                bandwidth,
                cancellationToken);

            if (networkId == Guid.Empty)
            {
                return new ApiResponse<List<CellTechnologyBandDto>>
                {
                    Message = $"Failed to create or retrieve Network for Technology '{techStandard}' and Band '{bandName}'. Check SystemType configuration.",
                    Succeeded = false
                };
            }

            comboNetworkMap[key] = (networkId, dlFreq, ulFreq, bandwidth);
        }

        // Create a set of desired technology-band-sector combinations
        var desiredCombinations = new HashSet<(Guid SectorId, Guid TechId, Guid BandId, Guid ProviderId)>();
        foreach (var sector in sectors)
        {
            foreach (var techBand in request.Payload.TechnologyBands)
            {
                var providerId = techBand.ServiceProviderId ?? defaultServiceProviderId;
                desiredCombinations.Add((sector.Id, techBand.TechnologyId, techBand.BandId, providerId));
            }
        }

        // Determine which Cellular records to delete (not in desired set)
        var cellularsToDelete = existingCellulars.Where(c =>
            !desiredCombinations.Contains((
                c.CellSectorId,
                c.TechnologyId,
                c.BandId,
                c.ServiceProviderId)))
            .ToList();

        foreach (var cellular in cellularsToDelete)
        {
            _cellularRepository.Delete(cellular);
            _logger.LogInformation(
                "Deleted Cellular record: SectorId={SectorId}, TechId={TechId}, BandId={BandId}",
                cellular.CellSectorId, cellular.TechnologyId, cellular.BandId);
        }

        // Determine which Cellular records to create (not already existing)
        var existingCombinations = new HashSet<(Guid SectorId, Guid TechId, Guid BandId, Guid ProviderId)>(
            existingCellulars.Select(c => (c.CellSectorId, c.TechnologyId, c.BandId, c.ServiceProviderId)));

        var cellularsToCreate = desiredCombinations
            .Where(combination => !existingCombinations.Contains(combination))
            .Select(combination =>
            {
                var comboKey = (combination.TechId, combination.BandId, combination.ProviderId);
                var (networkId, dlFreq, ulFreq, bw) = comboNetworkMap[comboKey];
                return new Domain.Entities.Cellular
                {
                    CellSectorId = combination.SectorId,
                    TechnologyId = combination.TechId,
                    BandId = combination.BandId,
                    ServiceProviderId = combination.ProviderId,
                    NetworkId = networkId,
                    DLCenterFreq = dlFreq,
                    ULCenterFreq = ulFreq,
                    Bandwidth = bw,
                    Address = cell.Address,
                    IsFileUploaded = false
                };
            })
            .ToList();

        foreach (var cellular in cellularsToCreate)
        {
            _cellularRepository.Add(cellular);
            _logger.LogInformation(
                "Created Cellular record: SectorId={SectorId}, TechId={TechId}, BandId={BandId}, NetworkId={NetworkId}",
                cellular.CellSectorId, cellular.TechnologyId, cellular.BandId, cellular.NetworkId);
        }

        // Save all changes
        await _cellularRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated technologies for Cell {CellId}: Added {AddedCount}, Deleted {DeletedCount}",
            request.CellId, cellularsToCreate.Count, cellularsToDelete.Count);

        // Create missing Sector rows for sectors that now have Cellular records
        var sectorsWithNewCellulars = cellularsToCreate
            .Select(c => c.CellSectorId)
            .Distinct()
            .ToList();

        foreach (var sectorId in sectorsWithNewCellulars)
        {
            var sectorExists = await _sectorRepository.CountAsync(
                _sectorRepository.Query(s => s.CellSectorId == sectorId),
                cancellationToken) > 0;

            if (!sectorExists)
            {
                var cellSector = sectors.First(s => s.Id == sectorId);
                var firstNetworkId = cellularsToCreate
                    .First(c => c.CellSectorId == sectorId)
                    .NetworkId;

                _sectorRepository.Add(new Domain.Entities.Sector
                {
                    CellSectorId = sectorId,
                    NetworkId = firstNetworkId,
                    AntennaType = cellSector.AntennaType,
                    TxAntennaHeight = cellSector.AntennaHeight,
                    TxAzimuth = cellSector.Azimuth,
                    TxBeamTilt = cellSector.BeamTilt,
                    RxAntennaHeight = cellSector.AntennaHeight,
                    RxAzimuth = cellSector.Azimuth,
                    RxBeamTilt = cellSector.BeamTilt,
                    IsRxEqualTx = true
                });
                _logger.LogInformation("Sector created for CellSectorId={CellSectorId}", sectorId);
            }
        }

        if (sectorsWithNewCellulars.Count > 0)
            await _sectorRepository.SaveChangesAsync(cancellationToken);

        // Retrieve and return the updated technology list
        var updatedQuery = _cellularRepository.Query()
            .Where(c => c.CellSector != null && c.CellSector.CellId == request.CellId)
            .Select(c => new
            {
                TechnologyId = c.TechnologyId,
                TechnologyStandard = c.Technology != null ? c.Technology.Standard : string.Empty,
                TechnologyDesc = c.Technology != null ? c.Technology.Description : null,
                BandId = c.BandId,
                BandName = c.Band != null ? c.Band.Name : string.Empty,
                BandDesc = c.Band != null ? c.Band.Description : null,
            })
            .Distinct();

        var updatedRows = await _cellularRepository.ToListAsync(updatedQuery, cancellationToken);

        var grouped = updatedRows
            .GroupBy(r => r.TechnologyId)
            .Select(g => new CellTechnologyBandDto
            {
                TechnologyId = g.Key,
                TechnologyStandard = g.First().TechnologyStandard,
                TechnologyDescription = g.First().TechnologyDesc,
                Bands = g
                    .DistinctBy(r => r.BandId)
                    .OrderBy(r => r.BandName)
                    .Select(r => new CellBandItemDto
                    {
                        BandId = r.BandId,
                        BandName = r.BandName,
                        BandDescription = r.BandDesc,
                    })
                    .ToList()
            })
            .OrderBy(t => t.TechnologyStandard)
            .ToList();

        return new ApiResponse<List<CellTechnologyBandDto>>
        {
            Data = grouped,
            Succeeded = true,
            Message = $"Cell technologies updated successfully. Added {cellularsToCreate.Count} and removed {cellularsToDelete.Count} technology-band assignments."
        };
    }

    private async Task<Guid> GetOrCreateNetworkAsync(
        Guid serviceProviderId,
        Guid? municipalityId,
        string municipalityName,
        Guid technologyId,
        Guid bandId,
        string technologyStandard,
        string bandName,
        decimal dlCenterFreq,
        decimal ulCenterFreq,
        decimal bandwidth,
        CancellationToken cancellationToken)
    {
        try
        {
            var networkNamePattern = $"{technologyStandard}_{bandName}_{municipalityName}";

            var existingNetworkId = await _networkRepository.FirstOrDefaultAsync(
                _networkRepository
                    .Query(n => n.ServiceProviderId == serviceProviderId && n.Name.StartsWith(networkNamePattern))
                    .Select(n => (Guid?)n.Id),
                cancellationToken);

            if (existingNetworkId.HasValue)
                return existingNetworkId.Value;

            var systemTypeId = await _systemTypeRepository.FirstOrDefaultAsync(
                _systemTypeRepository
                    .Query(st => st.TechnologyId == technologyId)
                    .Select(st => (Guid?)st.Id),
                cancellationToken);

            if (!systemTypeId.HasValue)
            {
                _logger.LogWarning(
                    "SystemType not found for TechnologyId: {TechnologyId} (Technology: {Technology})",
                    technologyId, technologyStandard);
                return Guid.Empty;
            }

            var newNetwork = new Domain.Aggregates.Network
            {
                Name = networkNamePattern,
                ServiceProviderId = serviceProviderId,
                SystemTypeId = systemTypeId.Value,
                BandId = bandId,
                MunicipalityId = municipalityId,
                ULCenterFreq = ulCenterFreq,
                DLCenterFreq = dlCenterFreq,
                Bandwidth = bandwidth
            };

            _networkRepository.Add(newNetwork);
            await _networkRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Network created: {NetworkName} (ServiceProviderId={ServiceProviderId}, TechnologyId={TechnologyId}, BandId={BandId})",
                newNetwork.Name, serviceProviderId, technologyId, bandId);

            return newNetwork.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating network for Technology={Technology}, Band={Band}",
                technologyStandard, bandName);
            return Guid.Empty;
        }
    }
}

