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
        var region = await _regionRepository.FindAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken);

        if (string.IsNullOrEmpty(request.Region.Name?.Trim()))
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

        var existingRegion = await _regionRepository.GetAllRegionsAsync(cancellationToken);
        if (existingRegion.Any(r => r.Id != region.Id &&
                                    r.Name != region.Name &&
                                    r.Description != region.Description))
        {
            return new ApiResponse<RegionDto>
            {
                Succeeded = false,
                Message = "Duplicate region found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Region with the same Name and Description already exists."]
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
            Message = "Request successful! Your changes have been saved.",
            Data = result
        };
    }
}
