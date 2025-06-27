using MediatR;
using NTC.QoS.Application.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Application.Features.Dashboard.Requests;

public class GetTopBarangaysInMunicipalityByCellCountRequest : IRequest<ApiResponse<List<GeoEntityCellCountDto>>>, ICacheableRequest
{
    public Guid? MunicipalityId { get; set; }
    public int Limit { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string CacheKey => $"top_barangays_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}_{MunicipalityId}_{Limit}";
    public TimeSpan? AbsoluteExpirationRelativeToNow => TimeSpan.FromHours(24);
    public TimeSpan? SlidingExpiration => null;
}