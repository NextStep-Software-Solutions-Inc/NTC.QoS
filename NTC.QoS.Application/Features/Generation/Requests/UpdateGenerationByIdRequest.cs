using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Generation.Requests;

public class UpdateGenerationByIdRequest : IRequest<ApiResponse<Domain.Aggregates.Generation>>
{
    public Guid Id { get; set; }
    public UpdateGenerationDto Generation { get; set; } = null!;

    public UpdateGenerationByIdRequest(Guid id, UpdateGenerationDto provider)
    {
        Id = id;
        Generation = Generation;
    }
}
