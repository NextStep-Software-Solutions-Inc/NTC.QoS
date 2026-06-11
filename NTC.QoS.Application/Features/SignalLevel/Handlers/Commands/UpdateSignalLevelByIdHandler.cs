using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.SignalLevel.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevel.Handlers.Commands;

public class UpdateSignalLevelByIdHandler(
    IRepository<Domain.Entities.SignalLevel> repository,
    IRepository<Domain.Entities.AreaStudy> areaStudyRepository) : IRequestHandler<UpdateSignalLevelByIdRequest, ApiResponse<SignalLevelDto>>
{
    private readonly IRepository<Domain.Entities.SignalLevel> _repository = repository;
    private readonly IRepository<Domain.Entities.AreaStudy> _areaStudyRepository = areaStudyRepository;

    public async Task<ApiResponse<SignalLevelDto>> Handle(UpdateSignalLevelByIdRequest request, CancellationToken cancellationToken)
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
                    { "Id", ["Signal level with the specified Id does not exist."] }
                }
            };
        }

        if (request.SignalLevel.AreaStudyId.HasValue && request.SignalLevel.AreaStudyId.Value != signalLevel.AreaStudyId)
        {
            var areaStudy = await _areaStudyRepository.FirstOrDefaultAsync(
                _areaStudyRepository.Query(a => a.Id == request.SignalLevel.AreaStudyId.Value),
                cancellationToken);

            if (areaStudy is null)
            {
                return new ApiResponse<SignalLevelDto>
                {
                    Succeeded = false,
                    Message = "Area study not found.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "AreaStudyId", ["Area study with the specified Id does not exist."] }
                    }
                };
            }

            signalLevel.AreaStudyId = request.SignalLevel.AreaStudyId.Value;
        }

        signalLevel.NoOfLevels = request.SignalLevel.NoOfLevels ?? signalLevel.NoOfLevels;

        _repository.Update(signalLevel);
        await _repository.SaveChangesAsync(cancellationToken);

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
            Succeeded = true,
            Message = "Signal level updated successfully."
        };
    }
}