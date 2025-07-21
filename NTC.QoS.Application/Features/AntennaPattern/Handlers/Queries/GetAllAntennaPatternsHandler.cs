using MediatR;
using NTC.QoS.Application.Features.AntennaPattern.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

namespace NTC.QoS.Application.Features.AntennaPattern.Handlers.Queries;

public class GetAllAntennaPatternsHandler (IRepository<Domain.Entities.AntennaPattern> repository) : IRequestHandler<GetAllAntennaPatternsRequest, PaginatedResponse<AntennaPatternDto>>
{
    private readonly IRepository<Domain.Entities.AntennaPattern> _repository = repository;
    public async Task<PaginatedResponse<AntennaPatternDto>> Handle(GetAllAntennaPatternsRequest request, CancellationToken cancellationToken)
    {
        var allAntennaPatternsQuery = _repository.Query().OrderBy(ap => ap.CreatedAt);

        var antennaPatternDtosQuery = allAntennaPatternsQuery.Select(ap => new AntennaPatternDto
        {
            Id = ap.Id,
            Name = ap.Name,
            URL = ap.URL
        });

        return await _repository.PaginateAsync(antennaPatternDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}