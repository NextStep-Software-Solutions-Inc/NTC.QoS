using MediatR;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Commands;

public class DeleteSpeedtestsByIdsHandler(IRepository<Domain.Entities.Speedtest> repository) : IRequestHandler<DeleteSpeedtestsByIdsRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Speedtest> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteSpeedtestsByIdsRequest request,
            CancellationToken cancellationToken)
    {
        var speedtestToDeleteQuery = _repository.Query(st => !st.IsDeleted && request.Ids.Contains(st.Id));
        var speedtestsToDelete = await _repository.ExecuteDeleteAsync(speedtestToDeleteQuery, cancellationToken);

        if (speedtestsToDelete is 0)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "None of the specified speedtest/s exist or are already deleted."
            };
        }  

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = $"{speedtestsToDelete} speedtest/s record deleted successfully."
        };
    }
}