using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Dashboard.Requests;

public class GetTotalNAPsCountRequest : IRequest<ApiResponse<int>>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
