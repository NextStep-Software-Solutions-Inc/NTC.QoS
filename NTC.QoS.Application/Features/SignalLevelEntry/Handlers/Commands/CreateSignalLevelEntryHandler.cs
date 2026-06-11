using MediatR;
using NTC.QoS.Application.Features.SignalLevelEntry.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevelEntry.Handlers.Commands;

public class CreateSignalLevelEntryHandler(
    IRepository<Domain.Entities.SignalLevelEntry> repository,
    IRepository<Domain.Entities.SignalLevel> signalLevelRepository) : IRequestHandler<CreateSignalLevelEntryRequest, ApiResponse<SignalLevelEntryDto>>
{
    private readonly IRepository<Domain.Entities.SignalLevelEntry> _repository = repository;
    private readonly IRepository<Domain.Entities.SignalLevel> _signalLevelRepository = signalLevelRepository;

    public async Task<ApiResponse<SignalLevelEntryDto>> Handle(CreateSignalLevelEntryRequest request, CancellationToken cancellationToken)
    {
        var signalLevel = await _signalLevelRepository.FirstOrDefaultAsync(
            _signalLevelRepository.Query(sl => sl.Id == request.SignalLevelId),
            cancellationToken);

        if (signalLevel is null)
        {
            return new ApiResponse<SignalLevelEntryDto>
            {
                Succeeded = false,
                Message = "Signal level not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "SignalLevelId", ["Signal level with the specified Id does not exist."] }
                }
            };
        }

        var currentEntryCount = await _repository.CountAsync(
            _repository.Query(sle => sle.SignalLevelId == request.SignalLevelId),
            cancellationToken);

        if (currentEntryCount >= signalLevel.NoOfLevels)
        {
            return new ApiResponse<SignalLevelEntryDto>
            {
                Succeeded = false,
                Message = "Signal level entry limit reached.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "SignalLevelId",
                        [$"This signal level already has {currentEntryCount} entries, which equals the configured maximum of {signalLevel.NoOfLevels}."]
                    }
                }
            };
        }

        var hasDuplicate = await _repository.CountAsync(
            _repository.Query(sle =>
                sle.SignalLevelId == request.SignalLevelId &&
                sle.ThresholdFrom == request.SignalLevelEntry.ThresholdFrom &&
                sle.ThresholdTo == request.SignalLevelEntry.ThresholdTo &&
                sle.SortOrder == request.SignalLevelEntry.SortOrder),
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

        var signalLevelEntry = new Domain.Entities.SignalLevelEntry
        {
            SignalLevelId = request.SignalLevelId,
            Color = request.SignalLevelEntry.Color,
            ThresholdFrom = request.SignalLevelEntry.ThresholdFrom,
            ThresholdTo = request.SignalLevelEntry.ThresholdTo,
            Description = request.SignalLevelEntry.Description,
            SortOrder = request.SignalLevelEntry.SortOrder
        };

        _repository.Add(signalLevelEntry);
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
            Message = "Signal level entry created successfully."
        };
    }
}