using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.SignalLevel.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevel.Handlers.Commands;

public class CreateSignalLevelHandler(
    IRepository<Domain.Entities.SignalLevel> repository,
    IRepository<Domain.Entities.AreaStudy> areaStudyRepository) : IRequestHandler<CreateSignalLevelRequest, ApiResponse<SignalLevelDto>>
{
    private readonly IRepository<Domain.Entities.SignalLevel> _repository = repository;
    private readonly IRepository<Domain.Entities.AreaStudy> _areaStudyRepository = areaStudyRepository;

    public async Task<ApiResponse<SignalLevelDto>> Handle(CreateSignalLevelRequest request, CancellationToken cancellationToken)
    {
        var areaStudy = await _areaStudyRepository.FirstOrDefaultAsync(
            _areaStudyRepository.Query(a => a.Id == request.SignalLevel.AreaStudyId),
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

        var signalLevel = new Domain.Entities.SignalLevel
        {
            AreaStudyId = request.SignalLevel.AreaStudyId,
            NoOfLevels = request.SignalLevel.NoOfLevels
        };

        _repository.Add(signalLevel);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<SignalLevelDto>
        {
            Data = new SignalLevelDto
            {
                Id = signalLevel.Id,
                AreaStudyId = signalLevel.AreaStudyId,
                NoOfLevels = signalLevel.NoOfLevels,
                SignalLevelEntries = []
            },
            Succeeded = true,
            Message = "Signal level created successfully."
        };
    }
}