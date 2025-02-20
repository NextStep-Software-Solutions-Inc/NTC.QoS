using MediatR;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Handlers.Commands
{
    public class CreateRegionRequestHandler(IRepository<Domain.Entities.Region> repository) : IRequestHandler<CreateRegionRequest, ApiResponse<CreateRegionDto>>
    {
        private readonly IRepository<Domain.Entities.Region> _repository = repository;

        public async Task<ApiResponse<CreateRegionDto>> Handle(CreateRegionRequest request, CancellationToken cancellationToken)
        {
            var region = await _repository.FindAsync(p => p.Name == request.Region.Name && !p.IsDeleted, cancellationToken);

            if (region != null)
            {
                return new ApiResponse<CreateRegionDto>
                {
                    Message = "Region already exists",
                    Succeeded = false
                };
            }

            var newRegion = new Domain.Entities.Region
            {
                Name = request.Region.Name,
                Description = request.Region.Description,
            };

            _repository.Add(newRegion);
            await _repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<CreateRegionDto>
            {
                Data = new CreateRegionDto
                {
                    Name = newRegion.Name,
                    Description = newRegion.Description,
                },

                Message = "Region created successfully.",
                Succeeded = true
            };
        }
    }
}