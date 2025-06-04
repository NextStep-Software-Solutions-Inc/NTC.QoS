using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Handlers.Queries;

public class GetRegionByIdHandler(IRegionRepository repository) : IRequestHandler<GetRegionByIdRequest, ApiResponse<RegionDto>>
{
    private readonly IRegionRepository _repository = repository;

    public async Task<ApiResponse<RegionDto>> Handle(GetRegionByIdRequest request, CancellationToken cancellationToken)
    {
        var regionQuery = _repository.Query(r => !r.IsDeleted && r.Id.Equals(request.Id));
        var region = await _repository.FirstOrDefaultAsync(regionQuery, cancellationToken);

        if (region is null)
        {
            return new ApiResponse<RegionDto>
            {
                Succeeded = false,
                Message = "Region with the specified Id does not exist.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullRegion", ["Region not found."]
                    }
                }
            };
        }

        return new ApiResponse<RegionDto>
        {
            Succeeded = true,
            Message = "Region retrieved successfully",
            Data = new RegionDto
            {
                Id = region.Id,
                Name = region.Name,
                Description = region.Description,
            }
        };
    }
}

