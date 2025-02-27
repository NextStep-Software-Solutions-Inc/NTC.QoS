using MediatR;
using NTC.QoS.Application.Features.AreaStudy.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.Application.Features.AreaStudy.Handlers.Queries;

public class GetAllAreaStudiesHandler(IRepository<Domain.Entities.AreaStudy> repository) : IRequestHandler<GetAllAreaStudiesRequest, PaginatedResponse<Domain.Entities.AreaStudy>>
{
    private readonly IRepository<Domain.Entities.AreaStudy> _repository = repository;   
    public async Task<PaginatedResponse<Domain.Entities.AreaStudy>> Handle(GetAllAreaStudiesRequest request, CancellationToken cancellationToken)
    {
        var areaStudies = await _repository.ListAsync(a => a.Name.Contains(request.Search ?? ""), cancellationToken);
        return new PaginatedResponse<Domain.Entities.AreaStudy>(areaStudies, request.PageNumber, request.PageSize);
    }
}