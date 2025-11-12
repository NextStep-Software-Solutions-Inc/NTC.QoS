using MediatR;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.Common;
using NTC.QoS.Domain.Models.RequestParams;

namespace NTC.QoS.Application.Features.Cells.Requests;

/// <summary>
/// Request for viewport-based cell queries (v2 API).
/// Uses ViewportQueryParams for bounding box filtering and pagination.
/// </summary>
public record GetAllCellsViewportRequest(ViewportQueryParams QueryParams) 
    : IRequest<PaginatedResponse<CellViewportDto>>
{
    public ViewportBounds Viewport => QueryParams.ToViewportBounds();
    public int PageNumber => QueryParams.PageNumber;
    public int PageSize => QueryParams.PageSize;
    public List<Guid>? ServiceProviderIds => QueryParams.ServiceProviderIds;
    public List<Guid>? BarangayIds => QueryParams.BarangayIds;
    public List<Guid>? MunicipalityIds => QueryParams.MunicipalityIds;
    public List<Guid>? ProvinceIds => QueryParams.ProvinceIds;
    public List<Guid>? RegionIds => QueryParams.RegionIds;
}
