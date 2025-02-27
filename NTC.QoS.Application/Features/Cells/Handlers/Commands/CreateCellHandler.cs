using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

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
            Data = new CellDto
            {
                Id = newCell.Id,
                Name = newCell.Name,
                Group = newCell.Group,
                FileId = newCell.FileId,
                File = new FileDto
                {
                    Id = newCell.File!.Id,
                    Name = newCell.File.Name,
                    Path = newCell.File.Path
                },
                Description = newCell.Description,
                ZoneId = newCell.ZoneId,
                Zone = new ZoneDto
                {
                    Name = newCell.Zone!.Name
                },
                ServiceProviderId = newCell.ServiceProviderId,
                ServiceProvider = new ServiceProviderDto
                {
                    Service = new Domain.Models.DTOs.Service.ServiceDto()
                    {
                        Name = newCell.ServiceProvider!.Service!.Name
                    },
                    Provider = new Domain.Models.DTOs.Provider.ProviderDto()
                    {
                        Name = newCell.ServiceProvider!.Provider!.Name
                    }
                },
                CID = newCell.CID,
                SiteId = newCell.SiteId,
                TowerType = newCell.TowerType,
                Elevation = newCell.Elevation,
                TAC = newCell.TAC,
                LAC = newCell.LAC,
                CoorLatitude = newCell.CoorLatitude,
                CoorLongitude = newCell.CoorLongitude,
                Status = EnumHelper<CellStatusEnum>.GetName( newCell.Status)
            },
            Message = "Cell created successfully",
            Succeeded = true

        };
    }
}