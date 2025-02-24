using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Models.DTOs.Generation;
using System.Net;

namespace NTC.QoS.Application.Features.Generation.Handlers.Queries;

public class GetGenerationByIdHandler(IRepository<Domain.Aggregates.Generation> repository) : IRequestHandler<GetGenerationByIdRequest, ApiResponse<GenerationDto>>
{
    private readonly IRepository<Domain.Aggregates.Generation> _repository = repository;

    public async Task<ApiResponse<GenerationDto>> Handle(GetGenerationByIdRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var generation = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (generation == null)
            {
                return new ApiResponse<GenerationDto>
                {
                    Succeeded = false,
                    Message = "Generation not found.",
                    Error = new[] { "Generation with the specified ID does not exist." },
                    StatusCode = (int)HttpStatusCode.NotFound
                };
            }

            var generationDto = new GenerationDto
            {
                Id = generation.Id,
                Name = generation.Name,
                Description = generation.Description,
            };

            return new ApiResponse<GenerationDto>
            {
                Succeeded = true,
                Message = "Generation retrieved successfully",
                Data = generationDto,
                StatusCode = (int)HttpStatusCode.OK
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<GenerationDto>
            {
                Succeeded = false,
                Message = "An unexpected error occurred.",
                Error = new[] { "Internal server error." },
                StatusCode = (int)HttpStatusCode.InternalServerError
            };
        }
    }
}