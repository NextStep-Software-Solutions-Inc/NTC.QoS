using MediatR;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.Cellular;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;
using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.Cellular.Handlers.Commands;

public class CreateCellularHandler(IRepository<Domain.Entities.Cellular> cellularRepository, ICellRepository cellRepository, IServiceProviderRepository serviceProviderRepository, IGenerationBandRepository generationBandRepository) : IRequestHandler<CreateCellularRequest, ApiResponse<CellularDto>>
{
    private readonly IRepository<Domain.Entities.Cellular> _cellularRepository = cellularRepository;
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly IServiceProviderRepository _serviceProviderRepository = serviceProviderRepository;
    private readonly IGenerationBandRepository _generationBandRepository = generationBandRepository;

    public async Task<ApiResponse<CellularDto>> Handle(CreateCellularRequest request, CancellationToken cancellationToken)
    {
        var cellularQuery = _cellularRepository.Query(c => c.CellId == request.Cellular.CellId
            && c.ServiceProviderId == request.Cellular.ServiceProviderId
            && c.GenerationBandId == request.Cellular.GenerationBandId
            && !c.IsDeleted
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

        var cellQuery = _cellRepository.Query(c => c.Id == request.Cellular.CellId && !c.IsDeleted);
        var cell = await _cellRepository.FirstOrDefaultAsync(cellQuery, cancellationToken);
        if (cell is null)
        {
            return new ApiResponse<CellularDto>
            {
                Message = "Cell not found.",
                Succeeded = false
            };
        }

        var serviceProviderQuery = _serviceProviderRepository.Query(sp => sp.Id == request.Cellular.ServiceProviderId && !sp.IsDeleted);
        var serviceProvider = await _serviceProviderRepository.FirstOrDefaultAsync(serviceProviderQuery, cancellationToken);
        if (serviceProvider is null)
        {
            return new ApiResponse<CellularDto>
            {
                Message = "Service provider not found.",
                Succeeded = false
            };
        }

        var generationBandQuery = _generationBandRepository.Query(gb => gb.Id == request.Cellular.GenerationBandId && !gb.IsDeleted);
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
            CellId = cell.Id,
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
                Cell = newCellular.Cell != null ? new CellDto()
                {
                    Id = newCellular.CellId,
                    Name = newCellular.Cell.Name,
                    Latitude = newCellular.Cell.Location.Coordinate.X,
                    Longitude = newCellular.Cell.Location.Coordinate.Y,
                    ServiceProvider = null,
                    LAC = newCellular.Cell.LAC,
                    CID = newCellular.Cell.CID,
                } : null,
                ServiceProvider = newCellular.ServiceProvider != null && newCellular.ServiceProvider.Provider != null && newCellular.ServiceProvider.Service != null ? new ServiceProviderDto()
                {
                    Id = newCellular.ServiceProviderId,
                    Provider = new ProviderDto()
                    {
                        Id = newCellular.ServiceProvider.ProviderId,
                        Name = newCellular.ServiceProvider.Provider.Name,
                        Color = newCellular.ServiceProvider.Provider.Color,
                    },
                    Service = new ServiceDto()
                    {
                        Id = newCellular.ServiceProvider.ServiceId,
                        Name = newCellular.ServiceProvider.Service.Name,
                        Type = newCellular.ServiceProvider.Service.Type.ToString(),
                    },
                } : null,
                GenerationBand =  newCellular.GenerationBand != null ? new GetGenerationBandDto()
                {
                    Id = newCellular.GenerationBandId,
                    Generation = new GenerationBandDto()
                    {
                        Id = newCellular.GenerationBand.GenerationId,
                    },
                    Band = new GenerationBandDto()
                    {
                        Id = newCellular.GenerationBand.BandId,
                    },
                } : null,
                Address = newCellular.Address
            },
            Message = "Cellular record created successfully.",
            Succeeded = true
        };
    }
}