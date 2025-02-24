using MediatR;
using NTC.QoS.Application.Features.AntennaPattern.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AntennaPattern;

namespace NTC.QoS.Application.Features.AntennaPattern.Handlers.Queries;

public class GetAllAntennaPatternsHandler (IRepository<Domain.Entities.AntennaPattern> repository) : IRequestHandler<GetAllAntennaPatternsRequest, PaginatedResponse<AntennaPatternDto>>
{
    private readonly IRepository<Domain.Entities.AntennaPattern> _repository = repository;
    public async Task<PaginatedResponse<AntennaPatternDto>> Handle(GetAllAntennaPatternsRequest request, CancellationToken cancellationToken)
    {
        var antennaPatterns = await _repository.ListAsync(ap => !ap.IsDeleted, cancellationToken);

        var AntennaPatternDto = antennaPatterns.Select(ap => new AntennaPatternDto
        {
            Id = ap.Id,
            Name = ap.Name,
            URL = ap.URL,
            CreatedBy = ap.CreatedBy,
            CreatedDate = ap.CreatedAt,
            ModifiedBy = ap.UpdatedBy,
            ModifiedDate = ap.UpdatedAt
        }).ToList();

        return new PaginatedResponse<AntennaPatternDto>(AntennaPatternDto, request.PageNumber, request.PageSize);
    }
}