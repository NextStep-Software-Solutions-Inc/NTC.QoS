using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetCellSitesByProviderHandler(ICellRepository cellRepository) : IRequestHandler<GetCellSitesByProviderRequest, ApiResponse<CellSitesByProviderResponseDto>>
{
    private readonly ICellRepository _cellRepository = cellRepository;

    public async Task<ApiResponse<CellSitesByProviderResponseDto>> Handle(
        GetCellSitesByProviderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cellRepository.GetCellSitesByProviderAsync(
            request.RegionId,
            request.ServiceProviderId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return new ApiResponse<CellSitesByProviderResponseDto>
        {
            Data = result,
            Succeeded = true,
            Message = "Cell sites by provider data retrieved successfully."
        };
    }
}
