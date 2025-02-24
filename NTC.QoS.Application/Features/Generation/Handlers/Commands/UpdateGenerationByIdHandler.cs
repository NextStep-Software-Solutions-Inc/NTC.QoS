using MediatR;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Generation;
using System.Net;

namespace NTC.QoS.Application.Features.Generation.Handlers.Commands
{
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
                    StatusCode = (int)HttpStatusCode.BadRequest
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
                    StatusCode = (int)HttpStatusCode.NotFound
                };
            }

            // Check for duplicate name
            var existingGeneration = await _repository.FindAsync(g => g.Name == request.Generation.Name && g.Id != request.Id && !g.IsDeleted, cancellationToken);
            if (existingGeneration != null)
            {
                return new ApiResponse<UpdateGenerationDto>
                {
                    Data = null,
                    Succeeded = false,
                    Error = new[] { "Generation name already exists." },
                    Message = $"Generation with name '{request.Generation.Name}' already exists.",
                    StatusCode = (int)HttpStatusCode.Conflict
                };
            }

            try
            {
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
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UpdateGenerationDto>
                {
                    Succeeded = false,
                    Error = new[] { $"An error occurred during update: {ex.Message}" },
                    Message = "Failed to update the generation.",
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }
    }
}