using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Handlers.Commands
{
    public class DeleteRegionRequestHandler : IRequestHandler<DeleteRegionRequest, ApiResponse<bool>>
    {
        private readonly IRepository<Domain.Entities.Region> _repository;

        public DeleteRegionRequestHandler(IRepository<Domain.Entities.Region> repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteRegionRequest request, CancellationToken cancellationToken)
        {
            var region = await _repository
                .FindAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken);

            if (region == null)
            {
                return new ApiResponse<bool>
                {
                    Succeeded = false,
                    Message = $"Region #{request.Id} not found.",
                    Data = false
                };
            }

            region.IsDeleted = true;

            _repository.Update(region);
            await _repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<bool>
            {
                Succeeded = true,
                Message = $"{region.Name} is successfully deleted.",
                Data = true
            };
        }
    }
}

