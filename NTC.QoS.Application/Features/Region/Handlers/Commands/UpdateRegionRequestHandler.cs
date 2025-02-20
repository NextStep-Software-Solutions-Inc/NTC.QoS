using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;
using NTC.QoS.Application.Features.Region.Requests;

namespace NTC.QoS.Application.Features.Region.Handlers.Commands
{
    public class UpdateRegionRequestHandler : IRequestHandler<UpdateRegionRequest, ApiResponse<UpdateRegionDto>>
    {
        private readonly IRepository<Domain.Entities.Region> _repository;

        public UpdateRegionRequestHandler(IRepository<Domain.Entities.Region> repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<UpdateRegionDto>> Handle(UpdateRegionRequest request, CancellationToken cancellationToken)
        {
            var region = await _repository.FindAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken);

            if (string.IsNullOrEmpty(request.Region.Name?.Trim()))
            {
                return new ApiResponse<UpdateRegionDto>
                {
                    Succeeded = false,
                    Error = new[] { "Name is required." },
                    Message = "Region name cannot be empty."
                };
            }

            if (region == null)
            {
                return new ApiResponse<UpdateRegionDto>
                {
                    Succeeded = false,
                    Error = new[] { "Region not found." },
                    Message = "The region you are trying to update does not exist."
                };
            }

            region.Name = request.Region.Name.Trim();

            _repository.Update(region);
            await _repository.SaveChangesAsync(cancellationToken);

            var regionDto = new RegionDto
            {
                Id = region.Id,
                Name = region.Name,
                Description = region.Description
            };

            return new ApiResponse<UpdateRegionDto>
            {
                Succeeded = true,
                Message = "Request successful! Your changes have been saved.",
                Data = request.Region,
            };
        }
    }
}
