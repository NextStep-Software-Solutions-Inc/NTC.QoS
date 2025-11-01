using MediatR;
using NTC.QoS.Domain.Models.DTOs.Common;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Domain.Models.RequestParams;

namespace NTC.QoS.Application.Features.Speedtest.Requests;

/// <summary>
/// Request for viewport-based speedtest queries (v2 API).
/// Uses ViewportQueryParams for bounding box filtering and pagination.
/// </summary>
public record GetAllSpeedtestsViewportRequest(ViewportQueryParams QueryParams) 
    : IRequest<PaginatedResponse<SpeedtestViewportDto>>
{
    public ViewportBoundsDto Viewport => QueryParams.Viewport;
    public int PageNumber => QueryParams.PageNumber;
    public int PageSize => QueryParams.PageSize;
    public List<Guid>? ServiceProviderIds => QueryParams.ServiceProviderIds;
    public List<Guid>? BarangayIds => QueryParams.BarangayIds;
    public List<Guid>? MunicipalityIds => QueryParams.MunicipalityIds;
    public List<Guid>? ProvinceIds => QueryParams.ProvinceIds;
    public List<Guid>? RegionIds => QueryParams.RegionIds;
}
