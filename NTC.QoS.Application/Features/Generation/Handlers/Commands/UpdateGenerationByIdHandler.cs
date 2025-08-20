using MediatR;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Generation.Handlers.Commands;

public class UpdateGenerationByIdHandler(IRepository<Domain.Aggregates.Generation> repository) : IRequestHandler<UpdateGenerationByIdRequest, ApiResponse<GenerationDto>>
{
    private readonly IRepository<Domain.Aggregates.Generation> _repository = repository;
    public async Task<ApiResponse<GenerationDto>> Handle(UpdateGenerationByIdRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Generation == null)
        {
            return new ApiResponse<GenerationDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Generation", ["Generation data is required."] }
                },
                Message = "The request does not contain generation update data.",
            };
        }

        var generationQuery = _repository.Query(g => g.Id == request.Id && !g.IsDeleted);
        var generation = await _repository.FirstOrDefaultAsync(generationQuery, cancellationToken);

        if (generation == null)
        {
            return new ApiResponse<GenerationDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Generation not found."] }
                },
                Message = "The generation you are trying to edit does not exist.",
            };
        }

        generation.Name = request.Generation.Name ?? generation.Name;
        generation.Description = request.Generation.Description;

        _repository.Update(generation);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<GenerationDto>
        {
            Succeeded = true,
            Data = new GenerationDto
            {
                Name = request.Generation.Name ?? string.Empty,
                Description = request.Generation.Description
            },
            Message = "Generation updated successfully.",
        };
    }
}
