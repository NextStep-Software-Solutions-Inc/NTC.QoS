using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetCellByIdHandler(ICellRepository cellRepository) : IRequestHandler<GetCellByIdRequest, ApiResponse<CellDto>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    public async Task<ApiResponse<CellDto>> Handle(GetCellByIdRequest request, CancellationToken cancellationToken)
    {
        var cellQuery = _cellRepository.Query(c => c.Id.Equals(request.Id));
        var cell = await _cellRepository.FirstOrDefaultAsync(cellQuery, cancellationToken);
        if (cell is null)
        {
            return new ApiResponse<CellDto>
            {
                Succeeded = false,
                Message = "Cell not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Cell with the specified ID does not exist."] }
                }
            };
        }

        return new ApiResponse<CellDto>
        {
            Succeeded = true,
            Message = "Cell retrieved successfully.",
            Data = new CellDto
            {
                Id = cell.Id,
                Name = cell.Name,
                Group = cell.Group,
                Description = cell.Description,
                Address = cell.Address,
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
                Latitude = cell.Location.Coordinate.Y,
                Longitude = cell.Location.Coordinate.X,
                IsVerified = cell.IsVerified,
                IsFileUploaded = cell.IsFileUploaded,
                Status = EnumHelper<CellStatusEnum>.GetName(cell.Status),
            }
        };
    }
}
