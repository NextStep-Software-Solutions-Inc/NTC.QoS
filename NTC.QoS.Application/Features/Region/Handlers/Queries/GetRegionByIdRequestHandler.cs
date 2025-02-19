using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Region.Requests;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Handlers.Queries;

public class GetRegionByIdRequestHandler : IRequestHandler<GetRegionByIdRequest, ApiResponse<RegionDto>>
{
    private readonly IRepository<Domain.Entities.Region> _repository;
    public GetRegionByIdRequestHandler(IRepository<Domain.Entities.Region> repository)
    {
        _repository = repository;
    }
    public async Task<ApiResponse<RegionDto>> Handle(GetRegionByIdRequest request, CancellationToken cancellationToken)
    {
        var region = await _repository.Find(p => p.Id == request.Id && p.IsDeleted != true)
            .Select(r => new RegionDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
        }).FirstOrDefaultAsync(cancellationToken);

        return new ApiResponse<RegionDto>
        {
            Succeeded = true,
            Message = "Region retrieved successfully.",
            Data = region
        };
    }
}

