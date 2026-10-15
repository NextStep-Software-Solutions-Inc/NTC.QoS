using MediatR;
using NTC.QoS.Application.Features.SignalLevelEntry.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.SignalLevelEntry.Handlers.Commands;

public class DeleteSignalLevelEntryByIdHandler(IRepository<Domain.Entities.SignalLevelEntry> repository) : IRequestHandler<DeleteSignalLevelEntryByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.SignalLevelEntry> _repository = repository;

    public async Task<ApiResponse<bool>> Handle(DeleteSignalLevelEntryByIdRequest request, CancellationToken cancellationToken)
    {
        var signalLevelEntryQuery = _repository.Query(sle => sle.Id == request.Id && sle.SignalLevelId == request.SignalLevelId);
        var signalLevelEntry = await _repository.FirstOrDefaultAsync(signalLevelEntryQuery, cancellationToken);

        if (signalLevelEntry is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "Signal level entry not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", [$"Signal level entry with ID [{request.Id}] does not exist."] }
                }
            };
        }

        _repository.Delete(signalLevelEntry);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>(true, true, "Signal level entry deleted successfully.");
    }
}