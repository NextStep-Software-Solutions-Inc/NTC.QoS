using MediatR;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Handlers.Commands;

public class CreateRegionRequestHandler(IRegionRepository repository) : IRequestHandler<CreateRegionRequest, ApiResponse<RegionDto>>
{
    private readonly IRegionRepository _repository = repository;

    public async Task<ApiResponse<RegionDto>> Handle(CreateRegionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Region.Name))
        {
            return new ApiResponse<RegionDto>
            {
                Message = "Region name is required.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { 
                        "Name", ["Region name cannot be empty."] 
                    }
                }
            };
        }

        var existingRegion = await _repository.FindAsync(p => p.Name == request.Region.Name && !p.IsDeleted, cancellationToken);

        if (existingRegion is not null)
        {
            return new ApiResponse<RegionDto>
            {
                Message = "Region already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Name", ["A region with the specified name already exists."]
                    }
                }
            };
        }

        var newRegion = new Domain.Entities.Region
        {
            Name = request.Region.Name.Trim(),
            Description = request.Region.Description,
        };

        _repository.Add(newRegion);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<RegionDto>
        {
            Data = new RegionDto
            {
                Id = newRegion.Id,
                Name = newRegion.Name,
                Description = newRegion.Description,
            },

            Message = "Region created successfully.",
            Succeeded = true
        };
    }
}