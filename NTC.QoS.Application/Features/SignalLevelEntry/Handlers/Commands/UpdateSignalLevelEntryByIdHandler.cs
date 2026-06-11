using MediatR;
using NTC.QoS.Application.Features.SignalLevelEntry.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevelEntry.Handlers.Commands;

public class UpdateSignalLevelEntryByIdHandler(IRepository<Domain.Entities.SignalLevelEntry> repository) : IRequestHandler<UpdateSignalLevelEntryByIdRequest, ApiResponse<SignalLevelEntryDto>>
{
    private readonly IRepository<Domain.Entities.SignalLevelEntry> _repository = repository;

    public async Task<ApiResponse<SignalLevelEntryDto>> Handle(UpdateSignalLevelEntryByIdRequest request, CancellationToken cancellationToken)
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

        var updatedColor = request.SignalLevelEntry.Color ?? signalLevelEntry.Color;
        var updatedThresholdFrom = request.SignalLevelEntry.ThresholdFrom ?? signalLevelEntry.ThresholdFrom;
        var updatedThresholdTo = request.SignalLevelEntry.ThresholdTo ?? signalLevelEntry.ThresholdTo;
        var updatedDescription = request.SignalLevelEntry.Description ?? signalLevelEntry.Description;
        var updatedSortOrder = request.SignalLevelEntry.SortOrder ?? signalLevelEntry.SortOrder;

        var hasDuplicate = await _repository.CountAsync(
            _repository.Query(sle =>
                sle.Id != request.Id &&
                sle.SignalLevelId == request.SignalLevelId &&
                sle.ThresholdFrom == updatedThresholdFrom &&
                sle.ThresholdTo == updatedThresholdTo &&
                sle.SortOrder == updatedSortOrder),
            cancellationToken) > 0;

        if (hasDuplicate)
        {
            return new ApiResponse<SignalLevelEntryDto>
            {
                Succeeded = false,
                Message = "Duplicate signal level entry is not allowed.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "SignalLevelEntry",
                        ["An identical signal level entry already exists for this signal level."]
                    }
                }
            };
        }

        signalLevelEntry.Color = updatedColor;
        signalLevelEntry.ThresholdFrom = updatedThresholdFrom;
        signalLevelEntry.ThresholdTo = updatedThresholdTo;
        signalLevelEntry.Description = updatedDescription;
        signalLevelEntry.SortOrder = updatedSortOrder;

        _repository.Update(signalLevelEntry);
        await _repository.SaveChangesAsync(cancellationToken);

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
            Message = "Signal level entry updated successfully."
        };
    }
}