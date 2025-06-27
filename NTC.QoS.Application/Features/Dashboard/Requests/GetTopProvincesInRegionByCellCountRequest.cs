using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Application.Features.Dashboard.Requests;

public class GetTopProvincesInRegionByCellCountRequest : IRequest<ApiResponse<List<GeoEntityCellCountDto>>>
{
    public Guid RegionId { get; set; }
    public int TopCount { get; set; }
}