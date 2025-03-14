using MediatR;
using NTC.QoS.Application.Features.Organization.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Organization;

namespace NTC.QoS.Application.Features.Organization.Handlers.Queries;

public class GetAllOrganizationsHandler(IRepository<Domain.Entities.Organization> repository) : IRequestHandler<GetAllOrganizationsRequest, PaginatedResponse<GetOrganizationDto>>
{
    private readonly IRepository<Domain.Entities.Organization> _repository = repository;
    public async Task<PaginatedResponse<GetOrganizationDto>> Handle(GetAllOrganizationsRequest request, CancellationToken cancellationToken)
    {
        var allOrganizations = await _repository.ListAsync(o => !o.IsDeleted, cancellationToken);

        var organizationDtos = allOrganizations.Select(o => new GetOrganizationDto
        {
            Id = o.Id,
            Name = o.Name,
            Description = o.Description
        }).ToList();

        return new PaginatedResponse<GetOrganizationDto>(organizationDtos, request.PageNumber, request.PageSize)
        {
            Message = "Organizations retrieved successfully.",
            Succeeded = true
        };
    }
}
