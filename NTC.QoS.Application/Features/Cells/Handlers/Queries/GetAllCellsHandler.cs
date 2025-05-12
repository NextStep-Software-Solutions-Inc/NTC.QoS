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
               c => c.Address
            );
            cellQuery = cellQuery.Where(searchExpression);
        }

        cellQuery = cellQuery
            .Where(c => string.IsNullOrEmpty(request.Status) || c.Status == EnumHelper<CellStatusEnum>.ParseUserInput(request.Status))
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
                Description = cell.Description ?? string.Empty,
                Address = cell.Address ?? string.Empty,
                ServiceProvider = new()
                {
                    Id = cell.ServiceProviderId,
                    Service = new()
                    {
                        Id = cell.ServiceProvider != null && cell.ServiceProvider.Service != null ? cell.ServiceProvider.Service.Id : Guid.Empty,
                        Name = cell.ServiceProvider != null && cell.ServiceProvider.Service != null ? cell.ServiceProvider.Service.Name : string.Empty,
                        Type = cell.ServiceProvider != null && cell.ServiceProvider.Service != null ? cell.ServiceProvider.Service.Type.ToString() : string.Empty
                    },
                    Provider = new()
                    {
                        Id = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.Id : Guid.Empty,
                        Name = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.Name : string.Empty,
                        Description = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.Description : null,
                        LicenseNumber = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.LicenseNumber : null,
                        Color = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.Color : string.Empty
                    }
                },
                CID = cell.CID,
                LAC = cell.LAC,
                TAC = cell.TAC,
                SiteId = cell.SiteId,
                TowerType = cell.TowerType,
                Elevation = cell.Elevation,
                Latitude = cell.Location.X,
                Longitude = cell.Location.Y,
                IsVerified = cell.IsVerified,
                Status = EnumHelper<CellStatusEnum>.GetName(cell.Status)
            });

        return await _repository.PaginateAsync(cellDtoQuery, request.PageNumber, request.PageSize, cancellationToken);

    }
}
