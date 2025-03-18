using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.Application.Features.Cellular.Requests;

public class GetCellularByIdRequest(Guid id) : IRequest<ApiResponse<GetCellularDto>>
{
    public Guid Id { get; set; } = id;
}