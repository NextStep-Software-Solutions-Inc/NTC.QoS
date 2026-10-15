using MediatR;
using NTC.QoS.Application.Features.SignalLevelEntry.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevelEntry.Handlers.Queries;

public class GetSignalLevelEntryByIdHandler(IRepository<Domain.Entities.SignalLevelEntry> repository) : IRequestHandler<GetSignalLevelEntryByIdRequest, ApiResponse<SignalLevelEntryDto>>
{
    private readonly IRepository<Domain.Entities.SignalLevelEntry> _repository = repository;

    public async Task<ApiResponse<SignalLevelEntryDto>> Handle(GetSignalLevelEntryByIdRequest request, CancellationToken cancellationToken)
    {
        var signalLevelEntryQuery = _repository.Query(sle => sle.Id == request.Id && sle.SignalLevelId == request.SignalLevelId);
        var signalLevelEntry = await _repository.FirstOrDefaultAsync(signalLevelEntryQuery, cancellationToken);

        if (signalLevelEntry is null)
        {
            return new ApiResponse<SignalLevelEntryDto>
            {
                Succeeded = false,
                Message = "Signal level entry not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Signal level entry does not exist."] }
                }
            };
        }

        return new ApiResponse<SignalLevelEntryDto>
        {
            Data = new SignalLevelEntryDto
            {
                Id = signalLevelEntry.Id,
                SignalLevelId = signalLevelEntry.SignalLevelId,
                Color = signalLevelEntry.Color,
                ThresholdFrom = signalLevelEntry.ThresholdFrom,
                ThresholdTo = signalLevelEntry.ThresholdTo,
                Description = signalLevelEntry.Description,
                SortOrder = signalLevelEntry.SortOrder
            },
            Succeeded = true,
            Message = "Signal level entry retrieved successfully."
        };
    }
}