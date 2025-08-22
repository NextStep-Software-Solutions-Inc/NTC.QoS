using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Application.Features.Dashboard.Requests;

public class GetCellSitesByProviderRequest : IRequest<ApiResponse<CellSitesByProviderResponseDto>>
{
    public Guid? RegionId { get; set; }
    public Guid? ServiceProviderId { get; set; }
    public int? Limit { get; set; } = null; 
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}