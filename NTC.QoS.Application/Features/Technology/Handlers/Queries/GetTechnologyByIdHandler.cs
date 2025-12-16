using MediatR;
using NTC.QoS.Application.Features.Technology.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Application.Features.Technology.Handlers.Queries;

public class GetTechnologyByIdHandler(ITechnologyRepository technologyRepository) : IRequestHandler<GetTechnologyByIdRequest, ApiResponse<TechnologyDto>>
{
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;

    public async Task<ApiResponse<TechnologyDto>> Handle(GetTechnologyByIdRequest request, CancellationToken cancellationToken)
    {
        var technologyQuery = _technologyRepository.Query(t => t.Id == request.Id);
        var technology = await _technologyRepository.FirstOrDefaultAsync(technologyQuery, cancellationToken);
        if (technology is null)
        {
            return new ApiResponse<TechnologyDto>
            {
                Succeeded = false,
                Message = "Technology standard not found."
            };
        }

        return new ApiResponse<TechnologyDto>
        {
            Succeeded = true,
            Message = "Technology standard retrieved successfully",
            Data = new TechnologyDto()
            {
                Id = technology.Id,
                Standard = technology.Standard,
                Description = technology.Description,
                Generation = new()
                {
                    Id = technology.Generation.Id,
                    Name = technology.Generation.Name,
                    Description = technology.Generation.Description
                }
            }
        };
    }
}
