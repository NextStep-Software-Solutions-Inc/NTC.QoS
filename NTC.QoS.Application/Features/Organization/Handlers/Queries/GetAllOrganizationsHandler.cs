using MediatR;
using NTC.QoS.Application.Features.Organization.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Organization;

namespace NTC.QoS.Application.Features.Organization.Handlers.Queries;

public class GetAllOrganizationsHandler(IRepository<Domain.Entities.Organization> repository) : IRequestHandler<GetAllOrganizationsRequest, PaginatedResponse<OrganizationDto>>
{
    private readonly IRepository<Domain.Entities.Organization> _organizationRepository = repository;
    public async Task<PaginatedResponse<OrganizationDto>> Handle(GetAllOrganizationsRequest request, CancellationToken cancellationToken)
    {
        var allOrganizations = _organizationRepository.Query();

        var organizationDtos = allOrganizations.Select(o => new OrganizationDto
        {
            Id = o.Id,
            Name = o.Name,
            Description = o.Description
        });

        return await _organizationRepository.PaginateAsync(organizationDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
