using MediatR;
using NTC.QoS.Application.Features.TechnologyBand.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.TechnologyBand;
using NTC.QoS.Domain.Models.DTOs.Band;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Application.Features.TechnologyBand.Handlers.Queries;

public class GetAllTechnologyBandsHandler(ITechnologyBandRepository repository) : IRequestHandler<GetAllTechnologyBandsRequest, PaginatedResponse<TechnologyBandDto>>
{
    private readonly ITechnologyBandRepository _repository = repository;

    public async Task<PaginatedResponse<TechnologyBandDto>> Handle(GetAllTechnologyBandsRequest request, CancellationToken cancellationToken)
    {
        var query = _repository.Query();

        if (request.Technologies is not null && request.Technologies.Count is not 0)
        {
            query = query.Where(tb => request.Technologies.Contains(tb.TechnologyId));
        }

        if (request.Bands is not null && request.Bands.Count is not 0)
        {
            query = query.Where(tb => request.Bands.Contains(tb.BandId));
        }

        var dtoQuery = query.Select(tb => new TechnologyBandDto
        {
            Id = tb.Id,
            Band = new BandDto
            {
                Id = tb.Band.Id,
                Name = tb.Band.Name,
                Description = tb.Band.Description
            },
            Technology = new TechnologyDto
            {
                Id = tb.Technology.Id,
                Standard = tb.Technology.Standard,
                Description = tb.Technology.Description,
                Generation = new()
                {
                    Id = tb.Technology.Generation.Id,
                    Name = tb.Technology.Generation.Name,
                    Description = tb.Technology.Generation.Description
                }
            }
        });
        return await _repository.PaginateAsync(dtoQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}