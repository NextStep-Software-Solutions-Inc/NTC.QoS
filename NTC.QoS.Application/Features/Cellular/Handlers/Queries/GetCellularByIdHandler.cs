using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Models.DTOs.Cellular;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;
using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.Cellular.Handlers.Queries;

public class GetCellularByIdHandler(ICellularRepository cellularRepository) : IRequestHandler<GetCellularByIdRequest, ApiResponse<CellularDto>>
{
    private readonly ICellularRepository _cellularRepository = cellularRepository;

    public async Task<ApiResponse<CellularDto>> Handle(GetCellularByIdRequest request, CancellationToken cancellationToken)
    {
        var cellularQuery = _cellularRepository.Query(c => !c.IsDeleted && c.Id.Equals(request.Id));
        var cellular = await _cellularRepository.FirstOrDefaultAsync(cellularQuery, cancellationToken);
        if (cellular is null)
        {
            return new ApiResponse<CellularDto>
            {
                Succeeded = false,
                Message = "Cellular record not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Cellular record with the specified ID does not exist."] }
                }
            };
        }

        return new ApiResponse<CellularDto>
        {
            Succeeded = true,
            Message = "Cellular record retrieved successfully.",
            Data = new CellularDto
            {
                Id = cellular.Id,
                Cell = cellular.Cell != null ? new CellDto()
                {
                    Id = cellular.CellId,
                    Name = cellular.Cell.Name,
                    Latitude = cellular.Cell.Location.Coordinate.X,
                    Longitude = cellular.Cell.Location.Coordinate.Y,
                    ServiceProvider = null,
                    LAC = cellular.Cell.LAC,
                    CID = cellular.Cell.CID,
                } : null,
                ServiceProvider = cellular.ServiceProvider != null && cellular.ServiceProvider.Provider != null && cellular.ServiceProvider.Service != null ? new ServiceProviderDto()
                {
                    Id = cellular.ServiceProviderId,
                    Provider = new ProviderDto()
                    {
                        Id = cellular.ServiceProvider.ProviderId,
                        Name = cellular.ServiceProvider.Provider.Name,
                        Color = cellular.ServiceProvider.Provider.Color,
                    },
                    Service = new ServiceDto()
                    {
                        Id = cellular.ServiceProvider.ServiceId,
                        Name = cellular.ServiceProvider.Service.Name,
                        Type = cellular.ServiceProvider.Service.Type.ToString(),
                    },
                } : null,
                GenerationBand = cellular.GenerationBand != null ? new GetGenerationBandDto()
                {
                    Id = cellular.GenerationBandId,
                    Generation = new GenerationBandDto()
                    {
                        Id = cellular.GenerationBand.GenerationId
                    },
                    Band = new GenerationBandDto()
                    {
                        Id = cellular.GenerationBand.BandId
                    },
                } : null,
                Address = cellular.Address
            }
        };
    }
}