using MediatR;
using NTC.QoS.Application.Features.Generation.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using System.Net;

namespace NTC.QoS.Application.Features.Generation.Handlers.Commands
{
    public class UpdateGenerationByIdHandler(IRepository<Domain.Aggregates.Generation> repository) : IRequestHandler<UpdateGenerationByIdRequest, ApiResponse<Domain.Aggregates.Generation>>
    {
        private readonly IRepository<Domain.Aggregates.Generation> _repository = repository;
        public async Task<ApiResponse<Domain.Aggregates.Generation>> Handle(UpdateGenerationByIdRequest request,
            CancellationToken cancellationToken)
        {
            var generation = await _repository.FindAsync(m => m.Id == request.Id && !m.IsDeleted, cancellationToken);

            if (string.IsNullOrEmpty(request.Generation.Name))
            {
                return new ApiResponse<Domain.Aggregates.Generation>
                {
                    Data = null,
                    Succeeded = false,
                    Error = new[] { "Name is required." },
                    Message = "Generation name cannot be empty.",
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }

            if (generation == null)
            {
                return new ApiResponse<Domain.Aggregates.Generation>
                {
                    Data = null,
                    Succeeded = false,
                    Error = new[] { "Generation not found." },
                    Message = "The generation you are trying to edit does not exist.",
                    StatusCode = (int)HttpStatusCode.NotFound
                };
            }

            // Check for duplicate name
            var existingGenerationWithSameName = await _repository.FindAsync(m => m.Name == request.Generation.Name && m.Id != request.Id && !m.IsDeleted, cancellationToken);
            if (existingGenerationWithSameName != null)
            {
                return new ApiResponse<Domain.Aggregates.Generation>
                {
                    Data = null,
                    Succeeded = false,
                    Error = new[] { "Generation name already exists." },
                    Message = $"Generation with name '{request.Generation.Name}' already exists.",
                    StatusCode = (int)HttpStatusCode.Conflict
                };
            }

            generation.Name = request.Generation.Name;
            generation.Description = request.Generation.Description;

            try
            {
                _repository.Update(generation);
                await _repository.SaveChangesAsync(cancellationToken);

                return new ApiResponse<Domain.Aggregates.Generation>
                {
                    Succeeded = true,
                    Error = Array.Empty<string>(),
                    Data = generation,
                    Message = "Request successful! Your changes have been saved.",
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Domain.Aggregates.Generation>
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