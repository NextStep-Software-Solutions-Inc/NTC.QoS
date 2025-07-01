using MediatR;
using NTC.QoS.Application.Features.Organization.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Organization;

namespace NTC.QoS.Application.Features.Organization.Handlers.Commands;

public class CreateOrganizationHandler(IRepository<Domain.Entities.Organization> repository): IRequestHandler<CreateOrganizationRequest, ApiResponse<OrganizationDto>>
{
    private readonly IRepository<Domain.Entities.Organization> _repository = repository;

    public async Task<ApiResponse<OrganizationDto>> Handle(CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var organizationQuery = _repository.Query(o => o.Name == request.Organization.Name);
        var organization = await _repository.FirstOrDefaultAsync(organizationQuery, cancellationToken);

        if (organization is not null)
        {
            return new ApiResponse<OrganizationDto>
            {
                Succeeded = false,
                Message = "Organization already exists."
            };
        }
                
        var newOrganization = new Domain.Entities.Organization
        {
            Name = request.Organization.Name,
            Description = request.Organization.Description,
        };

        _repository.Add(newOrganization);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<OrganizationDto>
        {
            Data = new OrganizationDto
            {
                Id = newOrganization.Id,
                Name = newOrganization.Name,
                Description = newOrganization.Description,
            },
            Succeeded = true,
            Message = "Organization created successfully.",
        };
    }
}
