using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenerationBand;

namespace NTC.QoS.Application.Features.GenerationBand.Requests;

public class CreateGenerationBandRequest : IRequest<ApiResponse<CreateGenerationBandDto>>
{
    public required CreateGenerationBandDto GenerationBand { get; set; }
}
