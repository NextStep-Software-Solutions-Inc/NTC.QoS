using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.SignalLevel.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevel.Handlers.Queries;

public class GetAllSignalLevelsHandler(IRepository<Domain.Entities.SignalLevel> repository) : IRequestHandler<GetAllSignalLevelsRequest, PaginatedResponse<SignalLevelDto>>
{
    private readonly IRepository<Domain.Entities.SignalLevel> _repository = repository;

    public async Task<PaginatedResponse<SignalLevelDto>> Handle(GetAllSignalLevelsRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Domain.Entities.SignalLevel> signalLevelsQuery = _repository.Query();
        signalLevelsQuery = signalLevelsQuery.Include(sl => sl.SignalLevelEntries);

        if (request.AreaStudyId.HasValue)
        {
            signalLevelsQuery = signalLevelsQuery.Where(sl => sl.AreaStudyId == request.AreaStudyId.Value);
        }

        var paginatedSignalLevels = await _repository.PaginateAsync(signalLevelsQuery, request.PageNumber, request.PageSize, cancellationToken);

        var signalLevelDtos = (paginatedSignalLevels.Data ?? []).Select(sl => new SignalLevelDto
        {
            Id = sl.Id,
            AreaStudyId = sl.AreaStudyId,
            NoOfLevels = sl.NoOfLevels,
            SignalLevelEntries = sl.SignalLevelEntries
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
        }).ToList();

        return new PaginatedResponse<SignalLevelDto>(signalLevelDtos, paginatedSignalLevels.PageNumber, paginatedSignalLevels.PageSize, paginatedSignalLevels.TotalRows);
    }
}