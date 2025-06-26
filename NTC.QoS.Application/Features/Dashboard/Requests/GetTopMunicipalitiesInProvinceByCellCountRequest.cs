using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Application.Features.Dashboard.Requests;

public class GetTopMunicipalitiesInProvinceByCellCountRequest : IRequest<ApiResponse<List<GeoEntityCellCountDto>>>
{
    public Guid? ProvinceId { get; set; }
    public int Limit { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}