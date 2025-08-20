using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Speedtest.Requests;

public class DeleteSpeedtestsByIdsRequest : IRequest<ApiResponse<bool>>
{
    public required List<Guid> Ids { get; set; }
}


