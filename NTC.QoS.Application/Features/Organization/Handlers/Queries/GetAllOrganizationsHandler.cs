using MediatR;
using NTC.QoS.Application.Features.Organization.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Organization;

namespace NTC.QoS.Application.Features.Organization.Handlers.Queries;

public class GetAllOrganizationsHandler(IRepository<Domain.Entities.Organization> repository) : IRequestHandler<GetAllOrganizationsRequest, PaginatedResponse<GetOrganizationDto>>
{
    private readonly IRepository<Domain.Entities.Organization> _organizationRepository = repository;
    public async Task<PaginatedResponse<GetOrganizationDto>> Handle(GetAllOrganizationsRequest request, CancellationToken cancellationToken)
    {
        var allOrganizations = _organizationRepository.Query(o => !o.IsDeleted);

        var organizationDtos = allOrganizations.Select(o => new GetOrganizationDto
        {
            Id = o.Id,
            Name = o.Name,
            Description = o.Description
        });

        return await _organizationRepository.PaginateAsync(organizationDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
