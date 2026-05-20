using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Cells.Requests;
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
    ILogger<UpdateCellTechnologiesBandHandler> logger)
    : IRequestHandler<UpdateCellTechnologiesBandRequest, ApiResponse<List<CellTechnologyBandDto>>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly ICellSectorRepository _cellSectorRepository = cellSectorRepository;
    private readonly IRepository<Domain.Entities.Cellular> _cellularRepository = cellularRepository;
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;
    private readonly IBandRepository _bandRepository = bandRepository;
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
        var technologyIds = request.TechnologyBands.TechnologyBands.Select(tb => tb.TechnologyId).Distinct().ToList();
        var bandIds = request.TechnologyBands.TechnologyBands.Select(tb => tb.BandId).Distinct().ToList();

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

        // Get existing Cellular records for this cell
        var existingCellularQuery = _cellularRepository.Query(c =>
            c.CellSector != null && c.CellSector.CellId == request.CellId);
        var existingCellulars = await _cellularRepository.ToListAsync(existingCellularQuery, cancellationToken);

        var sectorIds = sectors.Select(s => s.Id).ToList();
        var defaultServiceProviderId = cell.ServiceProviderId;

        // Create a set of desired technology-band-sector combinations
        var desiredCombinations = new HashSet<(Guid SectorId, Guid TechId, Guid BandId, Guid ProviderId)>();
        foreach (var sector in sectors)
        {
            foreach (var techBand in request.TechnologyBands.TechnologyBands)
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
            .Select(combination => new Domain.Entities.Cellular
            {
                CellSectorId = combination.SectorId,
                TechnologyId = combination.TechId,
                BandId = combination.BandId,
                ServiceProviderId = combination.ProviderId
            })
            .ToList();

        foreach (var cellular in cellularsToCreate)
        {
            _cellularRepository.Add(cellular);
            _logger.LogInformation(
                "Created Cellular record: SectorId={SectorId}, TechId={TechId}, BandId={BandId}",
                cellular.CellSectorId, cellular.TechnologyId, cellular.BandId);
        }

        // Save all changes
        await _cellularRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated technologies for Cell {CellId}: Added {AddedCount}, Deleted {DeletedCount}",
            request.CellId, cellularsToCreate.Count, cellularsToDelete.Count);

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
}
