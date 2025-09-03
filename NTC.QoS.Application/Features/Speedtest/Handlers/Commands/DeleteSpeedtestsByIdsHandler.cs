using MediatR;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Commands;

public class DeleteSpeedtestsByIdsHandler(IRepository<Domain.Entities.Speedtest> repository, IUserContextService userContext, ITimeProvider clock) : IRequestHandler<DeleteSpeedtestsByIdsRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Speedtest> _repository = repository;
    private readonly IUserContextService _userContext = userContext;
    private readonly ITimeProvider _clock = clock;
    public async Task<ApiResponse<bool>> Handle(DeleteSpeedtestsByIdsRequest request,
            CancellationToken cancellationToken)
    {
        var speedtestQuery = _repository.Query(st => request.Ids.Contains(st.Id));
        var deletedCount = await _repository.ExecuteHardDeleteAsync(speedtestQuery, cancellationToken);

        if (deletedCount is 0)
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
            Message = $"{deletedCount} speedtest/s record deleted successfully."
        };
    }
}