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
        var nAPsQuery = _napRepository.Query(nap => !nap.IsDeleted);

        if (request.StartDate.HasValue)
        {
            nAPsQuery = nAPsQuery.Where(nap => nap.CreatedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            nAPsQuery = nAPsQuery.Where(nap => nap.CreatedAt <= request.EndDate.Value);
        }

        var totalNAPs = nAPsQuery.Count();

        string message;
        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            message = $"Total NAP counts between ({request.StartDate.Value}) to ({request.EndDate.Value}) retrieved successfully.";
        }
        else if (request.StartDate.HasValue)
        {
            message = $"Total NAP counts from ({request.StartDate.Value}) onwards retrieved successfully.";
        }
        else if (request.EndDate.HasValue)
        {
            message = $"Total NAP counts up to ({request.EndDate.Value}) retrieved successfully.";
        }
        else
        {
            message = "Total NAP counts retrieved successfully (no date filter applied).";
        }

        return await Task.FromResult(new ApiResponse<int>
        {
            Data = totalNAPs,
            Succeeded = true,
            Message = message
        });
    }
}