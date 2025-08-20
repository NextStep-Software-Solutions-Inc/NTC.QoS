using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Band;

namespace NTC.QoS.Application.Features.Band.Requests;

public class CreateBandRequest : IRequest<ApiResponse<BandDto>>
{
    public required CreateBandDto Band { get; set; }
}