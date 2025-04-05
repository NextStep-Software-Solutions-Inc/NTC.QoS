using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetCellByIdHandler(ICellRepository cellRepository) : IRequestHandler<GetCellByIdRequest, ApiResponse<CellDto>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    public async Task<ApiResponse<CellDto>> Handle(GetCellByIdRequest request, CancellationToken cancellationToken)
    {
        var cellQuery = _cellRepository.Query(c => c.Id.Equals(request.Id));

        var cellDtoQuery = cellQuery.Select(cell => new CellDto
        {
            Id = cell.Id,
            Name = cell.Name,
            Group = cell.Group,
            Description = cell.Description,
            File = cell.File != null ? new()
            {
                Id = cell.File.Id,
                Name = cell.File.Name,
                Path = cell.File.Path
            } : null,
            Zone = cell.Zone != null ? new()
            {
                Id = cell.Zone.Id,
                Name = cell.Zone.Name,
                Barangay = cell.Zone.Barangay != null ? new()
                {
                    Id = cell.Zone.Barangay.Id,
                    Name = cell.Zone.Barangay.Name,
                    Description = cell.Zone.Barangay.Description,
                    Municipality = cell.Zone.Barangay.Municipality != null ? new()
                    {
                        Id = cell.Zone.Barangay.Municipality.Id,
                        IsCity = cell.Zone.Barangay.Municipality.IsCity,
                        Name = cell.Zone.Barangay.Municipality.Name,
                        Description = cell.Zone.Barangay.Municipality.Description,
                        Province = cell.Zone.Barangay.Municipality.Province != null ? new ()
                        {
                            Id = cell.Zone.Barangay.Municipality.Province.Id,
                            Name = cell.Zone.Barangay.Municipality.Province.Name,
                            Description = cell.Zone.Barangay.Municipality.Province.Description,
                            Region = cell.Zone.Barangay.Municipality.Province.Region != null ? new RegionDto()
                            {
                                Id = cell.Zone.Barangay.Municipality.Province.Region!.Id,
                                Name = cell.Zone.Barangay.Municipality.Province.Region!.Name,
                                Description = cell.Zone.Barangay.Municipality.Province.Region!.Description
                            } : null
                        } : null
                    } : null
                } : null
            } : null,
            ServiceProvider = cell.ServiceProvider != null ? new()
            {
                Id = cell.ServiceProvider.Id,
                Service = cell.ServiceProvider.Service != null ? new()
                {
                    Id = cell.ServiceProvider.Service.Id,
                    Name = cell.ServiceProvider.Service.Name,
                    Type = EnumHelper<ServiceTypeEnum>.GetName(cell.ServiceProvider.Service.Type),
                } : null,
                Provider = cell.ServiceProvider.Provider != null ? new()
                {
                    Id = cell.ServiceProvider.Provider.Id,
                    Name = cell.ServiceProvider.Provider.Name,
                    Description = cell.ServiceProvider.Provider.Description,
                    LicenseNumber = cell.ServiceProvider.Provider.LicenseNumber,
                    Color = cell.ServiceProvider.Provider.Color
                } : null
            } : null,
            CID = cell!.CID,
            LAC = cell.LAC,
            TAC = cell.TAC,
            SiteId = cell.SiteId,
            TowerType = cell.TowerType,
            Elevation = cell.Elevation,
            CoorLatitude = cell.CoorLatitude,
            CoorLongitude = cell.CoorLongitude,
            IsVerified = cell.IsVerified,
            Status = EnumHelper<CellStatusEnum>.GetName(cell.Status)
        });

        var cell = await _cellRepository.FirstOrDefaultAsync(cellDtoQuery, cancellationToken);

        return new ApiResponse<CellDto>
        {
            Data = cell,
            Succeeded = true,
            Message = "Cell retrieved successfully."
        };
    }
}
