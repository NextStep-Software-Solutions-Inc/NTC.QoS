using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;

namespace NTC.QoS.Application.Features.GenerationBand.Requests;

public class GetGenerationBandByIdRequest : IRequest<ApiResponse<GenerationBandDto>>
{
    public GetGenerationBandByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}