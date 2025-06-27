using MediatR;
using NTC.QoS.Application.Features.NetworkTemplate.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.NetworkTemplate.Handlers.Queries;

public class GetAllNetworkTemplatesHandler(IRepository<Domain.Entities.NetworkTemplate> repository) : IRequestHandler<GetAllNetworkTemplatesRequest, PaginatedResponse<NetworkTemplateDto>>
{
    private readonly IRepository<Domain.Entities.NetworkTemplate> _repository = repository;
    public async Task<PaginatedResponse<NetworkTemplateDto>> Handle(GetAllNetworkTemplatesRequest request, CancellationToken cancellationToken)
    {
        var allNetworkTemplates = _repository.Query(nt => !nt.IsDeleted);

        if (request.Search is not null)
        {
            allNetworkTemplates = allNetworkTemplates.Where(nt => nt.Name.Contains(request.Search));
        }

        var networkTemplates = allNetworkTemplates.Select(static allNetworkTemplate => new NetworkTemplateDto
        {
            Id = allNetworkTemplate.Id,
            Name = allNetworkTemplate.Name,
            Description = allNetworkTemplate.Description,
            URL = allNetworkTemplate.URL
        });

        return await _repository.PaginateAsync<NetworkTemplateDto>(networkTemplates, request.PageNumber, request.PageSize, cancellationToken);
    }
}
