using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class UpdateStatusByIdHandler(ICellRepository cellRepository) : IRequestHandler<UpdateCellStatusByIdRequest, ApiResponse<CellDto>>
{
    private readonly ICellRepository _cellRepository = cellRepository;

    public async Task<ApiResponse<CellDto>> Handle(UpdateCellStatusByIdRequest request, CancellationToken cancellationToken)
    {
        var cellQuery = _cellRepository.Query(c => c.Id.Equals(request.Id));
        var cell = await _cellRepository.FirstOrDefaultAsync(cellQuery, cancellationToken);
        if (cell is null)
        {
            return new ApiResponse<CellDto>
            {
                Succeeded = false,
                Message = "Cell not found",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullCell", ["Cell not found."]
                    }
                }
            };
        }

        cell.Status = EnumHelper<CellStatusEnum>.ParseUserInput(request.Status);

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
                TowerType = cell.TowerType.ToString(),
                Elevation = cell.Elevation,
                Latitude = cell.Location.Coordinate.Y,
                Longitude = cell.Location.Coordinate.X,
                IsVerified = cell.IsVerified,
                IsFileUploaded = cell.IsFileUploaded,
                Status = EnumHelper<CellStatusEnum>.GetName(cell.Status)
            },
            Succeeded = true,
            Message = "Cell status updated successfully."
        };
    }
}
