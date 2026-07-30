using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.SignalLevel.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevel.Handlers.Queries;

public class GetSignalLevelHandler(IRepository<Domain.Entities.SignalLevel> repository) : IRequestHandler<GetSignalLevelByIdRequest, ApiResponse<SignalLevelDto>>
{
    private readonly IRepository<Domain.Entities.SignalLevel> _repository = repository;

    public async Task<ApiResponse<SignalLevelDto>> Handle(GetSignalLevelByIdRequest request, CancellationToken cancellationToken)
    {
        var signalLevelQuery = _repository.Query(sl => sl.Id == request.Id).Include(sl => sl.SignalLevelEntries);
        var signalLevel = await _repository.FirstOrDefaultAsync(signalLevelQuery, cancellationToken);

        if (signalLevel is null)
        {
            return new ApiResponse<SignalLevelDto>
            {
                Succeeded = false,
                Message = "Signal level not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Signal level does not exist."] }
                }
            };
        }

        return new ApiResponse<SignalLevelDto>
        {
            Data = new SignalLevelDto
            {
                Id = signalLevel.Id,
                AreaStudyId = signalLevel.AreaStudyId,
                NoOfLevels = signalLevel.NoOfLevels,
                SignalLevelEntries = signalLevel.SignalLevelEntries
                    .OrderBy(e => e.SortOrder)
                    .Select(e => new SignalLevelEntryDto
                    {
                        Id = e.Id,
                        SignalLevelId = e.SignalLevelId,
                        Color = e.Color,
                        ThresholdFrom = e.ThresholdFrom,
                        ThresholdTo = e.ThresholdTo,
                        Description = e.Description,
                        SortOrder = e.SortOrder
                    })
                    .ToList()
            },
            Message = "Signal level retrieved successfully.",
            Succeeded = true
        };
    }
}