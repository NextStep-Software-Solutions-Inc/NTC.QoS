using MediatR;
using NTC.QoS.Application.Features.Band.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Band;

namespace NTC.QoS.Application.Features.Band.Handlers.Queries;

public class GetAllBandsHandler (IRepository<Domain.Entities.Band> repository) : IRequestHandler<GetAllBandsRequest, PaginatedResponse<BandDto>>
{
    private readonly IRepository<Domain.Entities.Band> _repository = repository;
    public async Task<PaginatedResponse<BandDto>> Handle(GetAllBandsRequest request, CancellationToken cancellationToken)
    {
        var tempQuery = _repository.Query();
        tempQuery = tempQuery.Where(b => request.Technologies == null || b.GenerationBands.Any(gb => gb.Generation.Technologies.Any(t => request.Technologies.Contains(t.Id))));

        var bandDtosQuery = tempQuery.Select(b => new BandDto
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
        });

        return await _repository.PaginateAsync(bandDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}