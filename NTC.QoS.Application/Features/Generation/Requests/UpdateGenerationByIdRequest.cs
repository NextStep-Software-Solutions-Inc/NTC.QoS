using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Generation.Requests;

public class UpdateGenerationByIdRequest : IRequest<ApiResponse<GenerationDto>>
{
    public Guid Id { get; set; }

    public required UpdateGenerationDto Generation { get; set; }
}
