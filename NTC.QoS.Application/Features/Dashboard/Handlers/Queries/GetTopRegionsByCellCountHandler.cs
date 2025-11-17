using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTopRegionsByCellCountHandler(IRegionRepository regionRepository) : IRequestHandler<GetTopRegionsByCellCountRequest, ApiResponse<List<GeoEntityCellCountDto>>>
{
    private readonly IRegionRepository _regionRepository = regionRepository;

    public async Task<ApiResponse<List<GeoEntityCellCountDto>>> Handle(GetTopRegionsByCellCountRequest request, CancellationToken cancellationToken)
    {
        var result = await _regionRepository.GetTopRegionsByCellCountAsync(
            request.Limit,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return new ApiResponse<List<GeoEntityCellCountDto>>
        {
            Succeeded = true,
            Message = "Top regions retrieved successfully.",
            Data = result
        };
    }
}
