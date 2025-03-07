using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.Application.Features.Cellular.Requests;

public class CreateCellularRequest : IRequest<ApiResponse<CreateCellularDto>>
{
    public required CreateCellularDto Cellular { get; set; }
}
