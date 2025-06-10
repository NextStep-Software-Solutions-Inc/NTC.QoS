using MediatR;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Generation.Handlers.Commands;

public class UpdateGenerationByIdHandler(IRepository<Domain.Aggregates.Generation> repository) : IRequestHandler<UpdateGenerationByIdRequest, ApiResponse<UpdateGenerationDto>>
{
    private readonly IRepository<Domain.Aggregates.Generation> _repository = repository;
    public async Task<ApiResponse<UpdateGenerationDto>> Handle(UpdateGenerationByIdRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Generation == null)
        {
            return new ApiResponse<UpdateGenerationDto>
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
            return new ApiResponse<UpdateGenerationDto>
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

        generation.Name = request.Generation.Name;
        generation.Description = request.Generation.Description;

        _repository.Update(generation);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UpdateGenerationDto>
        {
            Succeeded = true,
            Data = new UpdateGenerationDto
            {
                Name = request.Generation.Name,
                Description = request.Generation.Description
            },
            Message = "Request successful! Your changes have been saved.",
        };
    }
}
