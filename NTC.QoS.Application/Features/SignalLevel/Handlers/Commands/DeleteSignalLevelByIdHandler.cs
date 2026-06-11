using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.SignalLevel.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.SignalLevel.Handlers.Commands;

public class DeleteSignalLevelByIdHandler(IRepository<Domain.Entities.SignalLevel> repository) : IRequestHandler<DeleteSignalLevelByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.SignalLevel> _repository = repository;

    public async Task<ApiResponse<bool>> Handle(DeleteSignalLevelByIdRequest request, CancellationToken cancellationToken)
    {
        var signalLevelQuery = _repository.Query(sl => sl.Id == request.Id).Include(sl => sl.SignalLevelEntries);
        var signalLevel = await _repository.FirstOrDefaultAsync(signalLevelQuery, cancellationToken);

        if (signalLevel is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "Signal level not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", [$"Signal level with ID [{request.Id}] does not exist."] }
                }
            };
        }

        _repository.Delete(signalLevel);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>(true, true, "Signal level deleted successfully.");
    }
}