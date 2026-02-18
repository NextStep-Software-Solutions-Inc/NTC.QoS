using MediatR;
using NTC.QoS.Application.Features.AntennaPattern.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

namespace NTC.QoS.Application.Features.AntennaPattern.Handlers.Queries;

public class GetAllAntennaPatternsHandler(IRepository<Domain.Entities.AntennaPattern> repository) : IRequestHandler<GetAllAntennaPatternsRequest, PaginatedResponse<AntennaPatternDto>>
{
    private readonly IRepository<Domain.Entities.AntennaPattern> _repository = repository;
    public async Task<PaginatedResponse<AntennaPatternDto>> Handle(GetAllAntennaPatternsRequest request, CancellationToken cancellationToken)
    {
        var allAntennaPatternsQuery = _repository.Query();

        if (!string.IsNullOrEmpty(request.Search))
        {
            allAntennaPatternsQuery = _repository.ApplySearch(allAntennaPatternsQuery, request.Search, ap => ap.Name);
        }

        var antennaPatternDtosQuery = allAntennaPatternsQuery.OrderBy(ap => ap.CreatedAt).Select(ap => new AntennaPatternDto
        {
            Id = ap.Id,
            Name = ap.Name,
            URL = ap.URL
        });

        return await _repository.PaginateAsync(antennaPatternDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}