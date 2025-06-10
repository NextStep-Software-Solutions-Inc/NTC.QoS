using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;
using NTC.QoS.Application.Features.Region.Requests;

namespace NTC.QoS.Application.Features.Region.Handlers.Commands;

public class UpdateRegionHandler(IRegionRepository regionRepository) : IRequestHandler<UpdateRegionRequest, ApiResponse<RegionDto>>
{
    private readonly IRegionRepository _regionRepository = regionRepository;

    public async Task<ApiResponse<RegionDto>> Handle(UpdateRegionRequest request, CancellationToken cancellationToken)
    {
        var regionQuery = _regionRepository.Query(r => r.Id == request.Id && !r.IsDeleted);
        var region = await _regionRepository.FirstOrDefaultAsync(regionQuery, cancellationToken);

        if (request.Region is null || string.IsNullOrEmpty(request.Region.Name?.Trim()))
        {
            return new ApiResponse<RegionDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
        {
            {
                "Name", ["Region name is required."]
            }
        },
                Message = "Region name cannot be empty."
            };
        }

        if (region is null)
        {
            return new ApiResponse<RegionDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Id", ["The region you are trying to update does not exist."]
                    }
                },
                Message = "Region not found."
            };
        }

        region.Name = request.Region.Name.Trim();
        region.Description = request.Region.Description;

        var existingRegionQuery = _regionRepository.Query(
            r => r.Name == region.Name
                 && r.Id != request.Id
                 && !r.IsDeleted);
        var existingRegion = await _regionRepository.FirstOrDefaultAsync(existingRegionQuery, cancellationToken);

        if (existingRegion is not null)
        {
            return new ApiResponse<RegionDto>
            {
                Succeeded = false,
                Message = "A region with the same name already exists.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Region with the same name already exists."]
                    }
                }
            };
        }

        _regionRepository.Update(region);
        await _regionRepository.SaveChangesAsync(cancellationToken);

        var result = new RegionDto
        {
            Id = region.Id,
            Name = region.Name,
            Description = region.Description
        };

        return new ApiResponse<RegionDto>
        {
            Succeeded = true,
            Message = "Region updated successfully.",
            Data = result
        };
    }
}
