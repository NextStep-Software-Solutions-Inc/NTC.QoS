using MediatR;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Handlers.Queries;
public class GetAllRegionsHandler(IRegionRepository repository) : IRequestHandler<GetAllRegionsRequest, PaginatedResponse<RegionDto>>
{
    private readonly IRegionRepository _repository = repository;

    public async Task<PaginatedResponse<RegionDto>> Handle(GetAllRegionsRequest request, CancellationToken cancellationToken)
    {
        var allRegions = _repository.Query(r => !r.IsDeleted);

        var regionDtos = allRegions.Select(r => new RegionDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
        });

        return await _repository.PaginateAsync(regionDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}

