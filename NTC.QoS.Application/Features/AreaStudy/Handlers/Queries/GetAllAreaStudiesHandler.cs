using MediatR;
using NTC.QoS.Application.Features.AreaStudy.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.Application.Features.AreaStudy.Handlers.Queries;

public class GetAllAreaStudiesHandler(IRepository<Domain.Entities.AreaStudy> repository) : IRequestHandler<GetAllAreaStudiesRequest, PaginatedResponse<AreaStudyDto>>
{
    private readonly IRepository<Domain.Entities.AreaStudy> _repository = repository;

    public async Task<PaginatedResponse<AreaStudyDto>> Handle(GetAllAreaStudiesRequest request, CancellationToken cancellationToken)
    {
        var areaStudiesQuery = _repository.Query();

        if (!string.IsNullOrEmpty(request.Search))
        {
            areaStudiesQuery = areaStudiesQuery.Where(a => a.Name != null && a.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase));
        }

        var areaStudyDtosQuery = areaStudiesQuery.Select(a => new AreaStudyDto
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description
        });

        return await _repository.PaginateAsync(areaStudyDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
