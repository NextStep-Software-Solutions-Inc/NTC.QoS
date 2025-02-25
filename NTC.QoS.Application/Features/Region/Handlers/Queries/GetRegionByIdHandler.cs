using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Handlers.Queries;

public class GetRegionByIdHandler(IRepository<Domain.Entities.Region> repository) : IRequestHandler<GetRegionByIdRequest, ApiResponse<RegionDto>>
{
    private readonly IRepository<Domain.Entities.Region> _repository = repository;

    public async Task<ApiResponse<RegionDto>> Handle(GetRegionByIdRequest request, CancellationToken cancellationToken)
    {
        var region = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (region == null)
        {
            return new ApiResponse<RegionDto>
            {
                Succeeded = false,
                Message = "Region not found."
            };
        }

        return new ApiResponse<RegionDto>
        {
            Succeeded = true,
            Message = "Region retrieved successfully",
            Data = new RegionDto()
            {
                Id = region.Id,
                Name = region.Name,
                Description = region.Description,
            }
        };
    }
}

