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
        var cellDtoQuery = _cellRepository.Query(c => c.Id.Equals(request.Id))
            .Select(cell => new CellDto
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
                        Id = cell.ServiceProvider.Service.Id,
                        Name = cell.ServiceProvider.Service.Name,
                        Type = cell.ServiceProvider.Service.Type.ToString()
                    },
                    Provider = new()
                    {
                        Id = cell.ServiceProvider.Provider.Id,
                        Name = cell.ServiceProvider.Provider.Name,
                        Description = cell.ServiceProvider.Provider.Description,
                        LicenseNumber = cell.ServiceProvider.Provider.LicenseNumber,
                        Color = cell.ServiceProvider.Provider.Color
                    }
                },
                CID = cell.CID,
                LAC = cell.LAC,
                TAC = cell.TAC,
                SiteId = cell.SiteId,
                TowerType = cell.TowerType.ToString(),
                Elevation = cell.Elevation,
                Latitude = cell.Location.Coordinate.Y,
                Longitude = cell.Location.Coordinate.X,
                IsVerified = cell.IsVerified,
                IsFileUploaded = cell.IsFileUploaded,
                Status = EnumHelper<CellStatusEnum>.GetName(cell.Status)
            });

        var cellDto = await _cellRepository.FirstOrDefaultAsync(cellDtoQuery, cancellationToken);

        if (cellDto is null)
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
            Data = cellDto
        };
    }
}
