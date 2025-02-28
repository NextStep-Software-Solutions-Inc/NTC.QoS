using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Band;

namespace NTC.QoS.Application.Features.Band.Requests;

public class GetBandByIdRequest : IRequest<ApiResponse<BandDto>>
{
    public GetBandByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}