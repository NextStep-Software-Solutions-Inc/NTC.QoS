using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Handlers.Queries;

public class GetAllRegionsRequestHandler : IRequestHandler<GetAllRegionsRequest, PaginatedResponse<RegionDto>>
{
    private readonly IRepository<Domain.Entities.Region> _repository;

    public GetAllRegionsRequestHandler(IRepository<Domain.Entities.Region> repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedResponse<RegionDto>> Handle(GetAllRegionsRequest request, CancellationToken cancellationToken)
    {
        var allRegions = await _repository.GetAll(cancellationToken);
        var regions = allRegions.Select(r => new RegionDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            DateCreated = r.CreatedAt
        }).ToList();
        return new PaginatedResponse<RegionDto>(regions, request.PageNumber, request.PageSize);
    }
}


