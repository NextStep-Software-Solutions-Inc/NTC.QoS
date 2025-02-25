using MediatR;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Generation.Requests;

public class GetGenerationByIdRequest : IRequest<ApiResponse<GenerationDto>>
{
    public GetGenerationByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}