using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class UpdateCellByIdHandler(IRepository<Cell> repository) : IRequestHandler<UpdateCellByIdRequest, ApiResponse<Cell>>
{
    private readonly IRepository<Cell> _repository = repository;
    public async Task<ApiResponse<Cell>> Handle(UpdateCellByIdRequest request, CancellationToken cancellationToken)
    {
        var cellExists = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (cellExists == null)
        {
            return new ApiResponse<Cell>("Cell not found");
        }
        cellExists.Name = request.UpdateCell.Name;
        cellExists.Group = request.UpdateCell.Group;
        cellExists.Description = request.UpdateCell.Description;
        cellExists.FileId = request.UpdateCell.FileId;
        cellExists.ServiceProviderId = request.UpdateCell.ServiceProviderId;
        cellExists.ZoneId = request.UpdateCell.ZoneId;
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
        return new ApiResponse<Cell>(cellExists);
    }
}
