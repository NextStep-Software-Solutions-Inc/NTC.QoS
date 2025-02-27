using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetCellByIdHandler(ICellRepository cellRepository) : IRequestHandler<GetCellByIdRequest, ApiResponse<CellDto>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    public async Task<ApiResponse<CellDto>> Handle(GetCellByIdRequest request, CancellationToken cancellationToken)
    {
        var cellExists = await _cellRepository.GetByIdAsync(request.Id, cancellationToken);
        if (cellExists == null)
        {
            return new ApiResponse<CellDto>
            {
                Data = null,
                Message = "Cell not found",
                Succeeded = false
            };
        }
        var cell = new CellDto
        {
            Id = cellExists.Id,
            Name = cellExists.Name,
            Group = cellExists.Group,
            Description = cellExists.Description ?? "",
            FileId = cellExists.FileId,
            File = cellExists.File != null
                ? new FileDto
                {
                    Name = cellExists.File.Name ?? string.Empty,
                    Path = cellExists.File.Path ?? string.Empty
                }
                : null!,
            ServiceProviderId = cellExists.ServiceProviderId,
            ServiceProvider = cellExists.ServiceProvider != null
                ? new ServiceProviderDto
                {
                    Service = cellExists.ServiceProvider.Service != null ? new() { Name = cellExists.ServiceProvider.Service.Name ?? string.Empty } : null,
                    Provider = cellExists.ServiceProvider.Provider != null ? new() { Name = cellExists.ServiceProvider.Provider.Name ?? string.Empty } : null,
                }
                : null!,
            ZoneId = cellExists.ZoneId,
            Zone = cellExists.Zone != null
                ? new ZoneDto
                {
                    Id = cellExists.Zone.Id,
                    Name = cellExists.Zone.Name ?? string.Empty
                }
                : null!,
            CID = cellExists.CID,
            LAC = cellExists.LAC,
            TAC = cellExists.TAC,
            SiteId = cellExists.SiteId,
            TowerType = cellExists.TowerType,
            Elevation = cellExists.Elevation,
            CoorLatitude = cellExists.CoorLatitude,
            CoorLongitude = cellExists.CoorLongitude,
            IsVerified = cellExists.IsVerified,
            Status = EnumHelper<CellStatusEnum>.GetName(cellExists.Status)
        };

        return new ApiResponse<CellDto>(cell);
    }
}
