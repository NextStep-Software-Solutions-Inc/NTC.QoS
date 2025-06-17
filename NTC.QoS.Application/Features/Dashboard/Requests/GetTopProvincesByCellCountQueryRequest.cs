using MediatR;
using NTC.QoS.Domain.Models.DTOs.Dashboard_1;

namespace NTC.QoS.Application.Features.Dashboard.Requests;
public class GetTopProvincesByCellCountQueryRequest : IRequest<List<TopProvinceCellCountDto>>
{
    public Guid RegionId { get; set; }
    public int Top { get; set; } = 5;
}
