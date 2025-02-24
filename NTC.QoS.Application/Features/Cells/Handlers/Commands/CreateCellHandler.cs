using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Handlers.Commands;

public class CreateCellHandler(IRepository<Cell> cellRepository ) : IRequestHandler<CreateCellRequest, ApiResponse<CellDto>>
{
    private readonly IRepository<Cell> _cellRepository = cellRepository;
    public async Task<ApiResponse<CellDto>> Handle(CreateCellRequest request, CancellationToken cancellationToken)
    {
        var cell = await _cellRepository.FindAsync(c => c.Name == request.CreateCell.Name && !c.IsDeleted, cancellationToken);
        if (cell != null)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Cell already exists",
                Succeeded = false
            };
        }
        var newCell = new Cell
        {
            Name = request.CreateCell.Name,
            Group = request.CreateCell.Group,
            FileId = request.CreateCell.FileId,
            Description = request.CreateCell.Description,
            ZoneId = request.CreateCell.ZoneId,
            ServiceProviderId = request.CreateCell.ServiceProviderId,
            CID = request.CreateCell.CID,
            SiteId = request.CreateCell.SiteId,
            TowerType = request.CreateCell.TowerType,
            Elevation = request.CreateCell.Elevation,
            TAC = request.CreateCell.TAC,
            LAC = request.CreateCell.LAC,
            CoorLatitude = request.CreateCell.CoorLatitude,
            CoorLongitude = request.CreateCell.CoorLongitude
        };
        _cellRepository.Add(newCell);
        await _cellRepository.SaveChangesAsync();
        return new ApiResponse<CellDto>
        {
            Data = null,
            Message = "Cell created successfully",
            Succeeded = true

        };
    }
}