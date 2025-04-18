using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetAllCellsHandler(ICellRepository repository) : IRequestHandler<GetAllCellsRequest, PaginatedResponse<CellDto>>
{
    private readonly ICellRepository _repository = repository;

    public async Task<PaginatedResponse<CellDto>> Handle(GetAllCellsRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Cell> cellQuery = _repository.Query(c => !c.IsDeleted);

        if (!string.IsNullOrEmpty(request.Search))
        {
            var searchExpression = SearchHelper.BuildNullableKeywordSearchExpression<Cell>(
               request.Search,
               c => c.Name,
               c => c.Description,
               c => c.Group,
               c => c.Zone.Name,
               c => c.Zone.Barangay.Name,
               c => c.Zone.Barangay.Municipality.Name,
               c => c.Zone.Barangay.Municipality.Province.Name,
               c => c.Zone.Barangay.Municipality.Province.Region.Name
           );
            cellQuery = cellQuery.Where(searchExpression);
        }

        cellQuery = cellQuery
            .Where(c => string.IsNullOrEmpty(request.Status) || c.Status == EnumHelper<CellStatusEnum>.ParseUserInput(request.Status))
            .Where(c => request.FileIds == null || (request.FileIds.Count > 0 && request.FileIds.Contains(c.FileId)))
            .Where(c => request.ZoneIds == null || (request.ZoneIds.Count > 0 && request.ZoneIds.Contains(c.ZoneId)))
            .Where(c => request.ServiceProviderIds == null || (request.ServiceProviderIds.Count > 0 && request.ServiceProviderIds.Contains(c.ServiceProviderId)))
            .Where(c => request.ProviderIds == null || (request.ProviderIds.Count > 0 && (c.ServiceProvider != null && request.ProviderIds.Contains(c.ServiceProvider.ProviderId))))
            .Where(c => request.ServiceIds == null || (request.ServiceIds.Count > 0 && (c.ServiceProvider != null && request.ServiceIds.Contains(c.ServiceProvider.ServiceId))));

        var cellDtoQuery = cellQuery
            .OrderBy(c => c.CreatedAt)
            .Select(cell => new CellDto
        {
            Id = cell.Id,
            Name = cell.Name,
            Group = cell.Group,
            File = cell.File != null ? new()
            {
                Id = cell.File.Id,
                Name = cell.File.Name,
                Path = cell.File.Path
            } : null,
            Description = cell.Description,
            Zone = new ()
            {
                Id = cell.Zone.Id,
                Name = cell.Zone.Name,
                Barangay = new()
                {
                    Id = cell.Zone.Barangay.Id,
                    Name = cell.Zone.Barangay.Name,
                    Description = cell.Zone.Barangay.Description,
                    Municipality = new()
                    {
                        Id = cell.Zone.Barangay.Municipality.Id,
                        IsCity = cell.Zone.Barangay.Municipality.IsCity,
                        Name = cell.Zone.Barangay.Municipality.Name,
                        Description = cell.Zone.Barangay.Municipality.Description,
                        Province = new()
                        {
                            Id = cell.Zone.Barangay.Municipality.Province.Id,
                            Name = cell.Zone.Barangay.Municipality.Province.Name,
                            Description = cell.Zone.Barangay.Municipality.Province.Description,
                            Region = new()
                            {
                                Id = cell.Zone.Barangay.Municipality.Province.Region!.Id,
                                Name = cell.Zone.Barangay.Municipality.Province.Region!.Name,
                                Description = cell.Zone.Barangay.Municipality.Province.Region!.Description
                            }
                        },
                    },
                }
            },
            ServiceProvider = new()
            {
                Id = cell.ServiceProviderId,
                Service = new()
                {
                    Id = cell.ServiceProvider.ServiceId,
                    Name = cell.ServiceProvider.Service.Name,
                    Type = cell.ServiceProvider.Service.Type.ToString(),
                },
                Provider =new()
                {
                    Id = cell.ServiceProvider.ProviderId,
                    Name = cell.ServiceProvider.Provider.Name,
                    Color = cell.ServiceProvider.Provider.Color,
                    Description = cell.ServiceProvider.Provider.Description,
                    LicenseNumber = cell.ServiceProvider.Provider.LicenseNumber,
                },
                MNC = cell.ServiceProvider.MNC,
                MCC = cell.ServiceProvider.MCC,
            },
            CID = cell.CID,
            LAC = cell.LAC,
            TAC = cell.TAC,
            CoorLongitude = cell.CoorLongitude,
            CoorLatitude = cell.CoorLatitude,
            IsVerified = cell.IsVerified,
            Status = EnumHelper<CellStatusEnum>.GetName(cell.Status)
        });

        return await _repository.PaginateAsync(cellDtoQuery, request.PageNumber, request.PageSize, cancellationToken);

    }
}
