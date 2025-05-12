using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetCellByIdHandler(ICellRepository cellRepository) : IRequestHandler<GetCellByIdRequest, ApiResponse<CellDto>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    public async Task<ApiResponse<CellDto>> Handle(GetCellByIdRequest request, CancellationToken cancellationToken)
    {
        var cellQuery = _cellRepository.Query(c => c.Id.Equals(request.Id));

        var cellDtoQuery = cellQuery.Select(cell => new CellDto
        {
            Id = cell.Id,
            Name = cell.Name,
            Group = cell.Group,
            Description = cell.Description ?? string.Empty,
            Address = cell.Address ?? string.Empty,
            ServiceProvider = new()
            {
                Id = cell.ServiceProviderId,
                Service = new()
                {
                    Id = cell.ServiceProvider != null && cell.ServiceProvider.Service != null ? cell.ServiceProvider.Service.Id : Guid.Empty,
                    Name = cell.ServiceProvider != null && cell.ServiceProvider.Service != null ? cell.ServiceProvider.Service.Name : string.Empty,
                    Type = cell.ServiceProvider != null && cell.ServiceProvider.Service != null ? cell.ServiceProvider.Service.Type.ToString() : string.Empty
                },
                Provider = new()
                {
                    Id = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.Id : Guid.Empty,
                    Name = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.Name : string.Empty,
                    Description = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.Description : null,
                    LicenseNumber = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.LicenseNumber : null,
                    Color = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.Color : string.Empty
                }
            },
            CID = cell.CID,
            LAC = cell.LAC,
            TAC = cell.TAC,
            SiteId = cell.SiteId,
            TowerType = cell.TowerType,
            Elevation = cell.Elevation,
            Latitude = cell.Location.X,
            Longitude = cell.Location.Y,
            IsVerified = cell.IsVerified,
            Status = EnumHelper<CellStatusEnum>.GetName(cell.Status)
        });

        var cell = await _cellRepository.FirstOrDefaultAsync(cellDtoQuery, cancellationToken);

        return new ApiResponse<CellDto>
        {
            Data = cell,
            Succeeded = true,
            Message = "Cell retrieved successfully."
        };
    }
}
