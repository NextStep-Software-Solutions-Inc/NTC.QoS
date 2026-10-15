using MediatR;
using NTC.QoS.Application.Features.SignalLevelEntry.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.SignalLevel;

namespace NTC.QoS.Application.Features.SignalLevelEntry.Handlers.Queries;

public class GetAllSignalLevelEntriesHandler(IRepository<Domain.Entities.SignalLevelEntry> repository, IRepository<Domain.Entities.SignalLevel> signalLevelRepository) : IRequestHandler<GetAllSignalLevelEntriesRequest, PaginatedResponse<SignalLevelEntryDto>>
{
    private readonly IRepository<Domain.Entities.SignalLevelEntry> _repository = repository;
    private readonly IRepository<Domain.Entities.SignalLevel> _signalLevelRepository = signalLevelRepository;

    public async Task<PaginatedResponse<SignalLevelEntryDto>> Handle(GetAllSignalLevelEntriesRequest request, CancellationToken cancellationToken)
    {
        var signalLevel = await _signalLevelRepository.FirstOrDefaultAsync(
            _signalLevelRepository.Query(sl => sl.Id == request.SignalLevelId),
            cancellationToken);

        if (signalLevel is null)
        {
            return new PaginatedResponse<SignalLevelEntryDto>([], request.PageNumber, request.PageSize, 0)
            {
                Succeeded = false,
                Message = "Signal level not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "SignalLevelId", ["Signal level with the specified Id does not exist."] }
                }
            };
        }

        var signalLevelEntriesQuery = _repository.Query(sle => sle.SignalLevelId == request.SignalLevelId).OrderBy(sle => sle.SortOrder);
        var paginatedSignalLevelEntries = await _repository.PaginateAsync(signalLevelEntriesQuery, request.PageNumber, request.PageSize, cancellationToken);

        var signalLevelEntryDtos = (paginatedSignalLevelEntries.Data ?? []).Select(sle => new SignalLevelEntryDto
        {
            Id = sle.Id,
            SignalLevelId = sle.SignalLevelId,
            Color = sle.Color,
            ThresholdFrom = sle.ThresholdFrom,
            ThresholdTo = sle.ThresholdTo,
            Description = sle.Description,
            SortOrder = sle.SortOrder
        }).ToList();

        return new PaginatedResponse<SignalLevelEntryDto>(signalLevelEntryDtos, paginatedSignalLevelEntries.PageNumber, paginatedSignalLevelEntries.PageSize, paginatedSignalLevelEntries.TotalRows);
    }
}