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

        var existingRegion = await _regionRepository.FindAsync(
                r => r.Name == request.Region.Name
                     && r.Id != request.Id
                     && !r.IsDeleted,
                cancellationToken);

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
        region.Name = request.Region.Name.Trim();
        region.Description = request.Region.Description;

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
