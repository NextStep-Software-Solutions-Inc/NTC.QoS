using MediatR;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Province.Handlers.Queries;
public class GetAllProvincesHandler(IRepository<Domain.Entities.Province> repository) : IRequestHandler<GetAllProvincesRequest, PaginatedResponse<Domain.Entities.Province>>
{
    private readonly IRepository<Domain.Entities.Province> _repository = repository;

    public async Task<PaginatedResponse<Domain.Entities.Province>> Handle(GetAllProvincesRequest request, CancellationToken cancellationToken)
    {

        var allProvinces = await _repository.ListAsync(p => !p.IsDeleted, cancellationToken);

        return new PaginatedResponse<Domain.Entities.Province>(allProvinces, request.PageNumber, request.PageSize);
    }

}

