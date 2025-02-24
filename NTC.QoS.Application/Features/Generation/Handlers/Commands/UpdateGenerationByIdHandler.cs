using MediatR;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Interfaces;
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
                Error = new[] { "Generation data is missing." },
                Message = "The request does not contain generation update data.",
            };
        }

        var generation = await _repository.FindAsync(g => g.Id == request.Id && !g.IsDeleted, cancellationToken);

        if (generation == null)
        {
            return new ApiResponse<UpdateGenerationDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Generation not found." },
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
            Error = Array.Empty<string>(),
            Data = new UpdateGenerationDto
            {
                Name = request.Generation.Name,
                Description = request.Generation.Description
            },
            Message = "Request successful! Your changes have been saved.",
        };
    }
}
