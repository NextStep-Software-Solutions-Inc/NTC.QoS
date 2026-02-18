using MediatR;
using NTC.QoS.Application.Features.NetworkTemplate.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.NetworkTemplate.Handlers.Queries;

public class GetAllNetworkTemplatesHandler(IRepository<Domain.Entities.NetworkTemplate> repository) : IRequestHandler<GetAllNetworkTemplatesRequest, PaginatedResponse<NetworkTemplateDto>>
{
    private readonly IRepository<Domain.Entities.NetworkTemplate> _repository = repository;

    public async Task<PaginatedResponse<NetworkTemplateDto>> Handle(GetAllNetworkTemplatesRequest request, CancellationToken cancellationToken)
    {
        var allNetworkTemplates = _repository.Query();

        if (request.Search is not null)
        {
            allNetworkTemplates = _repository.ApplySearch(allNetworkTemplates, request.Search, nt => nt.Name);
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
