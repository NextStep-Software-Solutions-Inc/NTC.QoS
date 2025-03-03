using MediatR;
using NTC.QoS.Application.Features.Band.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Band;

namespace NTC.QoS.Application.Features.Band.Handlers.Queries;

public class GetAllBandsHandler (IRepository<Domain.Entities.Band> repository) : IRequestHandler<GetAllBandsRequest, PaginatedResponse<BandDto>>
{
    private readonly IRepository<Domain.Entities.Band> _repository = repository;
    public async Task<PaginatedResponse<BandDto>> Handle(GetAllBandsRequest request, CancellationToken cancellationToken)
    {
        var allBands = await _repository.ListAsync(b => b.IsDeleted != true, cancellationToken);

        var bandDtos = allBands.Select(b => new BandDto
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
            CreatedBy = b.CreatedBy,
            CreatedDate = b.CreatedAt,
            ModifiedBy = b.UpdatedBy,
            ModifiedDate = b.UpdatedAt
        }).ToList();

        return new PaginatedResponse<BandDto>(bandDtos, request.PageNumber, request.PageSize);
    }
}