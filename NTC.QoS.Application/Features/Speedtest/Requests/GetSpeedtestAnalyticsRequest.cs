using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;

namespace NTC.QoS.Application.Features.Speedtest.Requests;

public class GetSpeedtestAnalyticsRequest : IRequest<ApiResponse<SpeedtestAnalyticsDto>>
{
    public List<Guid>? BarangayIds { get; set; }
    public List<Guid>? MunicipalityIds { get; set; }
    public List<Guid>? ProvinceIds { get; set; }
    public List<Guid>? RegionIds { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
