using MediatR;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.Application.Features.Cellular.Handlers.Commands;

public class CreateCellularHandler(
    IRepository<Domain.Entities.Cellular> cellularRepository,
    ICellSectorRepository cellSectorRepository,
    IServiceProviderRepository serviceProviderRepository,
    ITechnologyRepository technologyRepository,
    IBandRepository bandRepository) : IRequestHandler<CreateCellularRequest, ApiResponse<CellularDto>>
{
    private readonly IRepository<Domain.Entities.Cellular> _cellularRepository = cellularRepository;
    private readonly ICellSectorRepository _cellSectorRepository = cellSectorRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;
    private readonly IBandRepository _bandRepository = bandRepository;

    public async Task<ApiResponse<CellularDto>> Handle(CreateCellularRequest request, CancellationToken cancellationToken)
    {
        var cellularQuery = _cellularRepository.Query(c => c.CellSectorId == request.Cellular.CellSectorId
            && c.ServiceProviderId == request.Cellular.ServiceProviderId
            && c.TechnologyId == request.Cellular.TechnologyId
            && c.BandId == request.Cellular.BandId
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

        var technologyQuery = _technologyRepository.Query(t => t.Id == request.Cellular.TechnologyId);
        var technology = await _technologyRepository.FirstOrDefaultAsync(technologyQuery, cancellationToken);
        if (technology is null)
        {
            return new ApiResponse<CellularDto>
            {
                Message = "Technology not found.",
                Succeeded = false
            };
        }

        var newCellular = new Domain.Entities.Cellular
        {
            CellSectorId = cellSector.Id,
            ServiceProviderId = serviceProvider.Id,
            TechnologyId = technology.Id,
            BandId = request.Cellular.BandId,
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
                Technology = newCellular.Technology != null ? new()
                {
                    Id = newCellular.TechnologyId,
                    Standard = newCellular.Technology.Standard,
                    Description = newCellular.Technology.Description
                } : null,
                Band = newCellular.Band != null ? new()
                {
                    Id = newCellular.BandId,
                    Name = newCellular.Band.Name,
                    Description = newCellular.Band.Description
                } : null
            },
            Message = "Cellular record created successfully.",
            Succeeded = true
        };
    }
}