using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Organization.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Organization;

namespace NTC.QoS.Application.Features.Organization.Handlers.Queries;

public class GetOrganizationByIdHandler(IRepository<Domain.Entities.Organization> repository) : IRequestHandler<GetOrganizationByIdRequest, ApiResponse<OrganizationDto>>
{
    private readonly IRepository<Domain.Entities.Organization> _repository = repository;

    public async Task<ApiResponse<OrganizationDto>> Handle(GetOrganizationByIdRequest request, CancellationToken cancellationToken)
    {
        var organizationQuery = _repository.Query(o => o.Id.Equals(request.Id));
        var organization = await _repository.FirstOrDefaultAsync(organizationQuery, cancellationToken);

        if (organization is null)
        {
            return new ApiResponse<OrganizationDto>
            {
                Succeeded = false,
                Message = "Organization not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Organization with the specified ID does not exist."] },
                },
            };
        }

        return new ApiResponse<OrganizationDto>
        {
            Succeeded = true,
            Message = "Organization retrieved successfully.",
            Data = new OrganizationDto
            {
                Id = organization.Id,
                Name = organization.Name,
                Description = organization.Description
            },
        };
    }
}
