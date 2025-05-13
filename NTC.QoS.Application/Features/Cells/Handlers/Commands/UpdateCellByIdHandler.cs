using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class UpdateCellByIdHandler(ICellRepository cellRepository) : IRequestHandler<UpdateCellByIdRequest, ApiResponse<CellDto>>
{
    private readonly ICellRepository _cellRepository = cellRepository;

    public async Task<ApiResponse<CellDto>> Handle(UpdateCellByIdRequest request, CancellationToken cancellationToken)
    {
        var cellQuery = _cellRepository.Query(c => !c.IsDeleted && c.Id.Equals(request.Id));
        var cell = await _cellRepository.FirstOrDefaultAsync(cellQuery, cancellationToken);
        if (cell is null)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Cell not found",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullCell", ["Cell not found."]
                    }
                }
            };
        }

        if(!string.IsNullOrEmpty(request.Cell.CID)) cell.CID = request.Cell.CID;
        if(!string.IsNullOrEmpty(request.Cell.LAC)) cell.LAC = request.Cell.LAC;
        if(!string.IsNullOrEmpty(request.Cell.TAC)) cell.TAC = request.Cell.TAC;
        if(!string.IsNullOrEmpty(request.Cell.SiteId)) cell.SiteId = request.Cell.SiteId;
        if(!string.IsNullOrEmpty(request.Cell.TowerType)) cell.TowerType = request.Cell.TowerType;
        if(request.Cell.Elevation is not 0) cell.Elevation = request.Cell.Elevation;
        if (request.Cell.Latitude is not 0 || request.Cell.Longitude is not 0)
        {
            cell.Location = GeometryHelper.CreatePoint(request.Cell.Latitude, request.Cell.Longitude);
        }
        if(request.Cell.IsVerified.HasValue) cell.IsVerified = request.Cell.IsVerified.Value;

        _cellRepository.Update(cell);
        await _cellRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CellDto>
        {
            Data = new CellDto
            {
                Id = cell.Id,
                Name = cell.Name,
                Group = cell.Group,
                Description = cell.Description ?? string.Empty,
                Address = cell.Address ?? string.Empty,
                ServiceProvider = new()
                {
                    Id = cell!.ServiceProviderId,
                    Service = new()
                    {
                        Id = cell.ServiceProvider?.Service?.Id ?? Guid.Empty,
                        Name = cell.ServiceProvider?.Service?.Name ?? string.Empty,
                        Type = cell.ServiceProvider?.Service?.Type.ToString() ?? string.Empty
                    },
                    Provider = new()
                    {
                        Id = cell.ServiceProvider?.Provider?.Id ?? Guid.Empty,
                        Name = cell.ServiceProvider?.Provider?.Name ?? string.Empty,
                        Description = cell.ServiceProvider?.Provider?.Description,
                        LicenseNumber = cell.ServiceProvider?.Provider?.LicenseNumber,
                        Color = cell.ServiceProvider?.Provider?.Color ?? string.Empty
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
            },
            Succeeded = true,
            Message = "Cell updated successfully."
        };
    }
}
