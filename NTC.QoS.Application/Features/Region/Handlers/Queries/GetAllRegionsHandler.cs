using MediatR;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Handlers.Queries;

public class GetAllRegionsHandler(IRegionRepository repository) : IRequestHandler<GetAllRegionsRequest, PaginatedResponse<RegionDto>>
{
    private readonly IRegionRepository _repository = repository;

    public async Task<PaginatedResponse<RegionDto>> Handle(GetAllRegionsRequest request, CancellationToken cancellationToken)
    {
        var allRegions = _repository.Query();

        var regionDtos = allRegions.OrderBy(r => r.Name).Select(r => new RegionDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            Code = r.Code,
            Area = r.Area,
        });

        return await _repository.PaginateAsync(regionDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
