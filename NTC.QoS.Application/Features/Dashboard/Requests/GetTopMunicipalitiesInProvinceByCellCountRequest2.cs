using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Application.Features.Dashboard.Requests;

public class GetTopMunicipalitiesInProvinceByCellCountRequest2 : IRequest<ApiResponse<List<TopMunicipalitiesInProvinceByCellCountDto>>>
{
    public Guid? ProvinceId { get; set; }
    public int Limit { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
