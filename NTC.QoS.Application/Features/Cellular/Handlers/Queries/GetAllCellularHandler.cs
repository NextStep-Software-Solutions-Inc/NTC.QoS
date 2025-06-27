using MediatR;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.Cellular;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;
using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.Cellular.Handlers.Queries;

public class GetAllCellularHandler(ICellularRepository cellularRepository) : IRequestHandler<GetAllCellularsRequest, PaginatedResponse<CellularDto>>
{
    private readonly ICellularRepository _cellularRepository = cellularRepository;
    public async Task<PaginatedResponse<CellularDto>> Handle(GetAllCellularsRequest request, CancellationToken cancellationToken)
    {
        var allCellularsQuery = _cellularRepository.Query(c => !c.IsDeleted);

        allCellularsQuery = allCellularsQuery
            .Where(c => !request.CellId.HasValue || c.CellId.Equals(request.CellId.Value))
            .Where(c => !request.ServiceProviderId.HasValue || c.ServiceProviderId.Equals(request.ServiceProviderId.Value))
            .Where(c => !request.GenerationBandId.HasValue || c.GenerationBandId.Equals(request.GenerationBandId.Value));

        var cellularDtosQuery = allCellularsQuery
            .Select(c => new CellularDto
            {
                Id = c.Id,
                Cell = c.Cell != null ? new CellDto()
                {
                    Id = c.CellId,
                    Name = c.Cell.Name,
                    Latitude = c.Cell.Location.Coordinate.X,
                    Longitude = c.Cell.Location.Coordinate.Y,
                    ServiceProvider = null,
                    LAC = c.Cell.LAC,
                    CID = c.Cell.CID,
                } : null,
                ServiceProvider = c.ServiceProvider != null && c.ServiceProvider.Provider != null && c.ServiceProvider.Service != null ? new ServiceProviderDto()
                {
                    Id = c.ServiceProviderId,
                    Provider = new ProviderDto()
                    {
                        Id = c.ServiceProvider.ProviderId,
                        Name = c.ServiceProvider.Provider.Name,
                        Color = c.ServiceProvider.Provider.Color,
                    },
                    Service = new ServiceDto()
                    {
                        Id = c.ServiceProvider.ServiceId,
                        Name = c.ServiceProvider.Service.Name,
                        Type = c.ServiceProvider.Service.Type.ToString(),
                    },
                } : null,
                GenerationBand = c.GenerationBand != null && c.GenerationBand.Generation != null && c.GenerationBand.Band != null ? new GetGenerationBandDto()
                {
                    Id = c.GenerationBandId,
                    Generation = new GenerationBandDto()
                    {
                        Id = c.GenerationBand.GenerationId,
                        Name = c.GenerationBand.Generation.Name,
                        Description = c.GenerationBand.Generation.Description,
                    },
                    Band = new GenerationBandDto()
                    {
                        Id = c.GenerationBand.BandId,
                        Name = c.GenerationBand.Band.Name,
                        Description = c.GenerationBand.Band.Description,
                    },
                } : null,
                Address = c.Address
            });

        return await _cellularRepository.PaginateAsync(cellularDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
