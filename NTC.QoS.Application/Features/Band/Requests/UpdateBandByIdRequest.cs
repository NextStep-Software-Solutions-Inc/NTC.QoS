using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Band;

namespace NTC.QoS.Application.Features.Band.Requests;

public class UpdateBandByIdRequest : IRequest<ApiResponse<UpdateBandDto>>
{
    public Guid Id { get; set; }

    public required UpdateBandDto Band { get; set; }
}
