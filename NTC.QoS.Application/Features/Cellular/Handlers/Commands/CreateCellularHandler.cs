using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cellular;
using Npgsql;

namespace NTC.QoS.Application.Features.Cellular.Handlers.Commands;

public class CreateCellularHandler(
    ICellularRepository cellularRepository,
    ICellSectorRepository cellSectorRepository,
    IServiceProviderRepository serviceProviderRepository,
    ITechnologyRepository technologyRepository,
    IBandRepository bandRepository,
    ISectorRepository sectorRepository,
    IMunicipalityRepository municipalityRepository,
    INetworkService networkService,
    ILogger<CreateCellularHandler> logger) : IRequestHandler<CreateCellularRequest, ApiResponse<CellularDto>>
{
    private readonly ICellularRepository _cellularRepository = cellularRepository;
    private readonly ICellSectorRepository _cellSectorRepository = cellSectorRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;
    private readonly IBandRepository _bandRepository = bandRepository;
    private readonly ISectorRepository _sectorRepository = sectorRepository;
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;
    private readonly INetworkService _networkService = networkService;
    private readonly ILogger<CreateCellularHandler> _logger = logger;

    public async Task<ApiResponse<CellularDto>> Handle(CreateCellularRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Load CellSector with Cell location via projection (no Include required)
            var cellSector = await _cellSectorRepository.FirstOrDefaultAsync(
                _cellSectorRepository
                    .Query(cs => cs.Id == request.Cellular.CellSectorId)
                    .Select(cs => new
                    {
                        cs.Id,
                        cs.AntennaType,
                        cs.AntennaHeight,
                        cs.Azimuth,
                        cs.BeamTilt,
                        CellLocation = cs.Cell.Location,
                        CellAddress = cs.Cell.Address
                    }),
                cancellationToken);

            if (cellSector is null)
            {
                return new ApiResponse<CellularDto>
                {
                    Message = "Cell Sector or its associated Cell not found.",
                    Succeeded = false
                };
            }

            // Step 2: Validate Service Provider exists
            var serviceProviderExists = await _serviceProviderRepository.CountAsync(
                _serviceProviderRepository.Query(sp => sp.Id == request.Cellular.ServiceProviderId),
                cancellationToken) > 0;

            if (!serviceProviderExists)
            {
                return new ApiResponse<CellularDto>
                {
                    Message = "Service provider not found.",
                    Succeeded = false
                };
            }

            // Step 3: Load Technology (Standard needed for frequency lookup)
            var technology = await _technologyRepository.FirstOrDefaultAsync(
                _technologyRepository
                    .Query(t => t.Id == request.Cellular.TechnologyId)
                    .Select(t => new { t.Id, t.Standard }),
                cancellationToken);

            if (technology is null)
            {
                return new ApiResponse<CellularDto>
                {
                    Message = "Technology not found.",
                    Succeeded = false
                };
            }

            // Step 4: Load Band (Name needed for frequency lookup)
            var band = await _bandRepository.FirstOrDefaultAsync(
                _bandRepository
                    .Query(b => b.Id == request.Cellular.BandId)
                    .Select(b => new { b.Id, b.Name }),
                cancellationToken);

            if (band is null)
            {
                return new ApiResponse<CellularDto>
                {
                    Message = "Band not found.",
                    Succeeded = false
                };
            }

            // Step 5: Duplicate check
            var isDuplicate = await _cellularRepository.CountAsync(
                _cellularRepository.Query(c =>
                    c.CellSectorId == request.Cellular.CellSectorId &&
                    c.ServiceProviderId == request.Cellular.ServiceProviderId &&
                    c.TechnologyId == request.Cellular.TechnologyId &&
                    c.BandId == request.Cellular.BandId),
                cancellationToken) > 0;

            if (isDuplicate)
            {
                return new ApiResponse<CellularDto>
                {
                    Message = "Cellular record with this combination already exists.",
                    Succeeded = false
                };
            }

            // Step 6: Resolve municipality from cell location (repository handles spatial query)
            var (municipalityId, municipalityName) = await _municipalityRepository
                .GetMunicipalityIdAndNameByLocationAsync(cellSector.CellLocation, cancellationToken);

            // Step 7: Calculate frequencies using shared FrequencyHelper
            var dlCenterFreq = FrequencyHelper.GetDefaultDLCenterFreq(technology.Standard, band.Name);
            var bandwidth = FrequencyHelper.GetDefaultBandwidth(technology.Standard);
            var ulCenterFreq = FrequencyHelper.CalculateULCenterFreq(dlCenterFreq, technology.Standard, band.Name);

            // Step 8: Get or create Network
            var networkId = await _networkService.GetOrCreateNetworkAsync(
                request.Cellular.ServiceProviderId,
                municipalityId,
                municipalityName ?? "Unknown",
                request.Cellular.TechnologyId,
                request.Cellular.BandId,
                technology.Standard,
                band.Name,
                dlCenterFreq,
                ulCenterFreq,
                bandwidth,
                cancellationToken);

            if (networkId == Guid.Empty)
            {
                return new ApiResponse<CellularDto>
                {
                    Message = "Failed to create or retrieve Network for this Technology/Band combination. Check SystemType configuration.",
                    Succeeded = false
                };
            }

            // Step 9: Create Cellular record with all required fields
            var newCellular = new Domain.Entities.Cellular
            {
                CellSectorId = cellSector.Id,
                ServiceProviderId = request.Cellular.ServiceProviderId,
                TechnologyId = request.Cellular.TechnologyId,
                BandId = request.Cellular.BandId,
                NetworkId = networkId,
                Address = request.Cellular.Address ?? cellSector.CellAddress,
                DLCenterFreq = dlCenterFreq,
                ULCenterFreq = ulCenterFreq,
                Bandwidth = bandwidth,
                IsFileUploaded = false
            };

            _cellularRepository.Add(newCellular);
            try
            {
                await _cellularRepository.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgresException &&
                                               postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                _cellularRepository.Delete(newCellular);
                return new ApiResponse<CellularDto>
                {
                    Message = "Cellular record with this combination already exists.",
                    Succeeded = false
                };
            }

            _logger.LogInformation(
                "Cellular record created: CellSectorId={CellSectorId}, Technology={Technology}, Band={Band}, NetworkId={NetworkId}",
                cellSector.Id, technology.Standard, band.Name, networkId);

            // Step 10: Create Sector if none exists for this CellSector
            var sectorExists = await _sectorRepository.CountAsync(
                _sectorRepository.Query(s => s.CellSectorId == cellSector.Id),
                cancellationToken) > 0;

            if (!sectorExists)
            {
                var newSector = new Domain.Entities.Sector
                {
                    CellSectorId = cellSector.Id,
                    NetworkId = networkId,
                    AntennaType = cellSector.AntennaType,
                    TxAntennaHeight = cellSector.AntennaHeight,
                    TxAzimuth = cellSector.Azimuth,
                    TxBeamTilt = cellSector.BeamTilt,
                    RxAntennaHeight = cellSector.AntennaHeight,
                    RxAzimuth = cellSector.Azimuth,
                    RxBeamTilt = cellSector.BeamTilt,
                    IsRxEqualTx = true
                };

                _sectorRepository.Add(newSector);

                try
                {
                    await _sectorRepository.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Sector created for CellSectorId={CellSectorId}", cellSector.Id);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgresException &&
                                                   postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    _sectorRepository.Delete(newSector);

                    var sectorCreatedByConcurrentRequest = await _sectorRepository.CountAsync(
                        _sectorRepository.Query(s => s.CellSectorId == cellSector.Id),
                        cancellationToken) > 0;

                    if (!sectorCreatedByConcurrentRequest)
                        throw;
                }
            }

            // Step 11: Return response using same Select projection as GetCellularByIdHandler
            var cellularDto = await _cellularRepository.FirstOrDefaultAsync(
                _cellularRepository
                    .Query(c => c.Id == newCellular.Id)
                    .Select(cellular => new CellularDto
                    {
                        Id = cellular.Id,
                        Address = cellular.Address,
                        CellSector = cellular.CellSector != null ? new()
                        {
                            Id = cellular.CellSectorId,
                            Cell = new()
                            {
                                Id = cellular.CellSector.Cell.Id,
                                Name = cellular.CellSector.Cell.Name,
                                Description = cellular.CellSector.Cell.Description,
                                Longitude = cellular.CellSector.Cell.Location.Coordinate.X,
                                Latitude = cellular.CellSector.Cell.Location.Coordinate.Y,
                                LAC = cellular.CellSector.Cell.LAC,
                                CID = cellular.CellSector.Cell.CID
                            },
                            AntennaType = cellular.CellSector.AntennaType.ToString(),
                            AntennaHeight = cellular.CellSector.AntennaHeight,
                            Azimuth = cellular.CellSector.Azimuth,
                            BeamTilt = cellular.CellSector.BeamTilt
                        } : null,
                        ServiceProvider = cellular.ServiceProvider != null ? new()
                        {
                            Id = cellular.ServiceProviderId,
                            Provider = new()
                            {
                                Id = cellular.ServiceProvider.ProviderId,
                                Name = cellular.ServiceProvider.Provider.Name,
                                Color = cellular.ServiceProvider.Provider.Color
                            },
                            Service = new()
                            {
                                Id = cellular.ServiceProvider.ServiceId,
                                Name = cellular.ServiceProvider.Service.Name,
                                Type = cellular.ServiceProvider.Service.Type.ToString()
                            }
                        } : null,
                        Technology = cellular.Technology != null ? new()
                        {
                            Id = cellular.TechnologyId,
                            Standard = cellular.Technology.Standard,
                            Description = cellular.Technology.Description
                        } : null,
                        Band = cellular.Band != null ? new()
                        {
                            Id = cellular.BandId,
                            Name = cellular.Band.Name,
                            Description = cellular.Band.Description
                        } : null
                    }),
                cancellationToken);

            return new ApiResponse<CellularDto>
            {
                Data = cellularDto,
                Message = "Cellular record created successfully.",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Cellular record for CellSectorId={CellSectorId}", request.Cellular.CellSectorId);
            throw;
        }
    }
}
