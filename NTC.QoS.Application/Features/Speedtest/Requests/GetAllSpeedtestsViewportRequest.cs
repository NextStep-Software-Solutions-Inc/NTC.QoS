using MediatR;
using NTC.QoS.Domain.Models.DTOs.Common;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Domain.Models.RequestParams;

namespace NTC.QoS.Application.Features.Speedtest.Requests;

/// <summary>
/// Request for viewport-based speedtest queries (v2 API).
/// Uses SpeedtestViewportQueryParams for bounding box filtering, speedtest filters, and pagination.
/// </summary>
public record GetAllSpeedtestsViewportRequest(SpeedtestViewportQueryParams QueryParams)
    : IRequest<PaginatedResponse<SpeedtestViewportDto>>
{
    public ViewportBounds Viewport => QueryParams.ToViewportBounds();
    public int PageNumber => QueryParams.PageNumber;
    public int PageSize => QueryParams.PageSize;
    public List<Guid>? ServiceProviderIds => QueryParams.ServiceProviderIds;
    public List<Guid>? BarangayIds => QueryParams.BarangayIds;
    public List<Guid>? MunicipalityIds => QueryParams.MunicipalityIds;
    public List<Guid>? ProvinceIds => QueryParams.ProvinceIds;
    public List<Guid>? RegionIds => QueryParams.RegionIds;
    public decimal? MinDownloadSpeed => QueryParams.MinDownloadSpeed;
    public decimal? MaxDownloadSpeed => QueryParams.MaxDownloadSpeed;
    public decimal? MinUploadSpeed => QueryParams.MinUploadSpeed;
    public decimal? MaxUploadSpeed => QueryParams.MaxUploadSpeed;
    public DateTime? StartDate => QueryParams.StartDate;
    public DateTime? EndDate => QueryParams.EndDate;
}
