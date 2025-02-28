using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class UpdateCellByIdHandler(IRepository<Cell> repository) : IRequestHandler<UpdateCellByIdRequest, ApiResponse<CellDto>>
{
    private readonly IRepository<Cell> _repository = repository;
    public async Task<ApiResponse<CellDto>> Handle(UpdateCellByIdRequest request, CancellationToken cancellationToken)
    {
        var cellExists = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (cellExists == null)
        {
            return new ApiResponse<CellDto>("Cell not found");
        }

        cellExists.CID = request.UpdateCell.CID;
        cellExists.LAC = request.UpdateCell.LAC;
        cellExists.TAC = request.UpdateCell.TAC;
        cellExists.SiteId = request.UpdateCell.SiteId;
        cellExists.TowerType = request.UpdateCell.TowerType;
        cellExists.Elevation = request.UpdateCell.Elevation;
        cellExists.CoorLatitude = request.UpdateCell.CoorLatitude;
        cellExists.CoorLongitude = request.UpdateCell.CoorLongitude;

        _repository.Update(cellExists);
        await _repository.SaveChangesAsync(cancellationToken);

        var newCell = new CellDto
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
        return new ApiResponse<CellDto>(newCell);
    }
}
