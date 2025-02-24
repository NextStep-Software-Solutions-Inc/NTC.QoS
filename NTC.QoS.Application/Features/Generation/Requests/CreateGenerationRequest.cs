using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Generation.Requests;

public class CreateGenerationRequest : IRequest<ApiResponse<CreateGenerationDto>>
{
    public CreateGenerationDto Generation { get; set; } = new();
}