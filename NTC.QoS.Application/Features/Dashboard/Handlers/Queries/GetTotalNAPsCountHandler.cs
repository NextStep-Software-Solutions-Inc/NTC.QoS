using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTotalNAPsCountHandler(INAPRepository napRepository) : IRequestHandler<GetTotalNAPsCountRequest, ApiResponse<int>>
{
    private readonly INAPRepository _napRepository = napRepository;

    public async Task<ApiResponse<int>> Handle(GetTotalNAPsCountRequest request, CancellationToken cancellationToken)
    {
        var nAPs = _napRepository.Query(c => !c.IsDeleted && 
            c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate).Count();

        return await Task.FromResult(new ApiResponse<int>
        {
            Data = nAPs,
            Succeeded = true,
            Message = "Total N A P count retrieved successfully."
        });
    }
}
