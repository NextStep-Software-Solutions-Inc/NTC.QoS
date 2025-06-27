using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Application.Features.Dashboard.Requests;

public class GetTopRegionsByCellCountRequest : IRequest<ApiResponse<List<GeoEntityCellCountDto>>>
{
    public int TopCount { get; set; }
}