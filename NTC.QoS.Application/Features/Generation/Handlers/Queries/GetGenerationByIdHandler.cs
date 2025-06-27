using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Generation.Handlers.Queries;

public class GetGenerationByIdHandler(IRepository<Domain.Aggregates.Generation> repository) : IRequestHandler<GetGenerationByIdRequest, ApiResponse<GenerationDto>>
{
    private readonly IRepository<Domain.Aggregates.Generation> _repository = repository;

    public async Task<ApiResponse<GenerationDto>> Handle(GetGenerationByIdRequest request, CancellationToken cancellationToken)
    {
        var generationQuery = _repository.Query(g => !g.IsDeleted && g.Id.Equals(request.Id));
        var generation = await _repository.FirstOrDefaultAsync(generationQuery, cancellationToken);

        if (generation == null)
        {
            return new ApiResponse<GenerationDto>
            {
                Succeeded = false,
                Message = "Generation not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {"Id", ["Generation with the specified ID does not exist."] }
                }
            };
        }

        return new ApiResponse<GenerationDto>
        {
            Succeeded = true,
            Message = "Generation retrieved successfully",
            Data = new GenerationDto
            {
                Id = generation.Id,
                Name = generation.Name,
                Description = generation.Description,
                CreatedDate = generation.CreatedAt,
                CreatedBy = generation.CreatedBy,
                ModifiedDate = generation.UpdatedAt,
                ModifiedBy = generation.UpdatedBy,
            },
        };
    }
}