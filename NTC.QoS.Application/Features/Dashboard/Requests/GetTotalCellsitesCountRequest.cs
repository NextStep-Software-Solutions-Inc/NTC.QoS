using MediatR;
using NTC.QoS.Application.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Application.Features.Dashboard.Requests;

public class GetTotalCellsitesCountRequest : IRequest<ApiResponse<CellsiteCountsDto>>, ICacheableRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string CacheKey => $"total_cellsites_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}";
    public TimeSpan? AbsoluteExpirationRelativeToNow => TimeSpan.FromHours(24);
    public TimeSpan? SlidingExpiration => null;
}
