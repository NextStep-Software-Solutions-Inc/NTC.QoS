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
        var zoneExist = await _cellRepository.FindAsync(c => c.Zone!.Id == request.CreateCell.ZoneId);
        if (zoneExist == null)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Zone does not exist",
                Succeeded = false
            };
        }
        var serviceProviderExist = await _cellRepository.FindAsync(c => c.ServiceProvider!.Id == request.CreateCell.ServiceProviderId);
        if (serviceProviderExist == null)
        {
            return new ApiResponse<CellDto>
            {
                Message = "Service provider does not exist",
                Succeeded = false
            };
        }
        var FileExist = await _cellRepository.FindAsync(c => c.File!.Id == request.CreateCell.FileId);
        if (FileExist == null)
        {
            return new ApiResponse<CellDto>
            {
                Message = "File does not exist",
                Succeeded = false
            };
        }
            var cell = new Cell
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
        _cellRepository.Add(cell);
        await _cellRepository.SaveChangesAsync();
        return new ApiResponse<CellDto>
        {
            Data = new CellDto
            {
                Id = cell.Id,
                Name = cell.Name,
                Group = cell.Group,
                FileId = cell.FileId,
                Description = cell.Description,
                ZoneId = cell.ZoneId,
                ServiceProviderId = cell.ServiceProviderId,
                CID = cell.CID,
                SiteId = cell.SiteId,
                TowerName = cell.TowerType,
                Elevation = cell.Elevation,
                TAC = cell.TAC,
                LAC = cell.LAC,
                CoorLatitude = cell.CoorLatitude,
                CoorLongitude = cell.CoorLongitude,
                IsVerified = cell.IsVerified,
                Status = cell.Status.ToString()
            },
            Message = "Cell created successfully",
            Succeeded = true

        };
    }
}