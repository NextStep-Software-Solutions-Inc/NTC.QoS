using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetCellByIdHandler(IRepository<Cell> cellRepository) : IRequestHandler<GetCellByIdRequest, ApiResponse<CellDto>>
{
    private readonly IRepository<Cell> _cellRepository = cellRepository;
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
            ServiceProviderId = cellExists.ServiceProviderId,
            ZoneId = cellExists.ZoneId,
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
