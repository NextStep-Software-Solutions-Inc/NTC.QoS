using MediatR;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.Application.Features.Cellular.Handlers.Commands;

public class CreateCellularHandler(IRepository<Domain.Entities.Cellular> cellularRepository, ICellSectorRepository cellSectorRepository, IServiceProviderRepository serviceProviderRepository, IGenerationBandRepository generationBandRepository) : IRequestHandler<CreateCellularRequest, ApiResponse<CellularDto>>
{
    private readonly IRepository<Domain.Entities.Cellular> _cellularRepository = cellularRepository;
    private readonly ICellSectorRepository _cellSectorRepository = cellSectorRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IGenerationBandRepository _generationBandRepository = generationBandRepository;

    public async Task<ApiResponse<CellularDto>> Handle(CreateCellularRequest request, CancellationToken cancellationToken)
    {
        var cellularQuery = _cellularRepository.Query(c => c.CellSectorId == request.Cellular.CellSectorId
            && c.ServiceProviderId == request.Cellular.ServiceProviderId
            && c.GenerationBandId == request.Cellular.GenerationBandId
        );
        var existingCellular = await _cellularRepository.FirstOrDefaultAsync(cellularQuery, cancellationToken);
        if (existingCellular is null)
        {
            return new ApiResponse<CellularDto>
            {
                Message = "Cellular already exists.",
                Succeeded = false
            };
        }

        var cellSectorQuery = _cellSectorRepository.Query(c => c.Id == request.Cellular.CellSectorId);
        var cellSector = await _cellSectorRepository.FirstOrDefaultAsync(cellSectorQuery, cancellationToken);
        if (cellSector is null)
        {
            return new ApiResponse<CellularDto>
            {
                Message = "Cell Sector not found.",
                Succeeded = false
            };
        }

        var serviceProviderQuery = _serviceProviderRepository.Query(sp => sp.Id == request.Cellular.ServiceProviderId);
        var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken);
        if (serviceProvider is null)
        {
            return new ApiResponse<CellularDto>
            {
                Message = "Service provider not found.",
                Succeeded = false
            };
        }

        var generationBandQuery = _generationBandRepository.Query(gb => gb.Id == request.Cellular.GenerationBandId);
        var generationBand = await _generationBandRepository.FirstOrDefaultAsync(generationBandQuery, cancellationToken);
        if (generationBand is null)
        {
            return new ApiResponse<CellularDto>
            {
                Message = "Generation band not found.",
                Succeeded = false
            };
        }

        var newCellular = new Domain.Entities.Cellular
        {
            CellSectorId = cellSector.Id,
            ServiceProviderId = serviceProvider.Id,
            GenerationBandId = generationBand.Id,
            Address = request.Cellular.Address,
        };

        _cellularRepository.Add(newCellular);
        await _cellularRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CellularDto>
        {
            Data = new CellularDto
            {
                Id = newCellular.Id,
                Address = newCellular.Address,
                CellSector = newCellular.CellSector != null ? new()
                {
                    Id = newCellular.CellSectorId,
                    Cell = new()
                    {
                        Id = newCellular.CellSector.Cell.Id,
                        Name = newCellular.CellSector.Cell.Name,
                        Description = newCellular.CellSector.Cell.Description,
                        Longitude = newCellular.CellSector.Cell.Location.Coordinate.X,
                        Latitude = newCellular.CellSector.Cell.Location.Coordinate.Y,
                        LAC = newCellular.CellSector.Cell.LAC,
                        CID = newCellular.CellSector.Cell.CID,
                    },
                    AntennaType = newCellular.CellSector.AntennaType.ToString(),
                    AntennaHeight = newCellular.CellSector.AntennaHeight,
                    Azimuth = newCellular.CellSector.Azimuth,
                    BeamTilt = newCellular.CellSector.BeamTilt
                } : null,
                ServiceProvider = newCellular.ServiceProvider != null && newCellular.ServiceProvider.Provider != null && newCellular.ServiceProvider.Service != null ? new()
                {
                    Id = newCellular.ServiceProviderId,
                    Provider = new()
                    {
                        Id = newCellular.ServiceProvider.ProviderId,
                        Name = newCellular.ServiceProvider.Provider.Name,
                        Color = newCellular.ServiceProvider.Provider.Color,
                    },
                    Service = new()
                    {
                        Id = newCellular.ServiceProvider.ServiceId,
                        Name = newCellular.ServiceProvider.Service.Name,
                        Type = newCellular.ServiceProvider.Service.Type.ToString(),
                    },
                } : null,
                GenerationBand = newCellular.GenerationBand != null ? new()
                {
                    Id = newCellular.GenerationBandId,
                    Generation = new()
                    {
                        Id = newCellular.GenerationBand.Generation.Id,
                        Name = newCellular.GenerationBand.Generation.Name,
                        Description = newCellular.GenerationBand.Generation.Description
                    },
                    Band = new()
                    {
                        Id = newCellular.GenerationBand.Band.Id,
                        Name = newCellular.GenerationBand.Band.Name,
                        Description = newCellular.GenerationBand.Band.Description
                    },
                } : null
            },
            Message = "Cellular record created successfully.",
            Succeeded = true
        };
    }
}