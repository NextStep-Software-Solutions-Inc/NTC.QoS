using MediatR;
using NTC.QoS.Application.Features.Reports.Dashboard.DTOs;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Reports.Dashboard.Queries;

public class GetDashboardReportQuery : IRequest<ApiResponse<DashboardReportResult>>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DaysInPast { get; set; }
    public Guid? RegionId { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? MunicipalityId { get; set; }
    public int Limit { get; set; } = 5;
}
