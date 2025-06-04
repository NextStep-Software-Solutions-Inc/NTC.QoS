using MediatR;
using NTC.QoS.Application.Features.Organization.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Organization;

namespace NTC.QoS.Application.Features.Organization.Handlers.Commands;

public class UpdateOrganizationByIdHandler(IRepository<Domain.Entities.Organization> repository) : IRequestHandler<UpdateOrganizationByIdRequest, ApiResponse<UpdateOrganizationDto>>
{
    private readonly IRepository<Domain.Entities.Organization> _repository = repository;

    public async Task<ApiResponse<UpdateOrganizationDto>> Handle(UpdateOrganizationByIdRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Organization is null)
        {
            return new ApiResponse<UpdateOrganizationDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Organization", ["Organization data is required."] }
                },
                Message = "The request does not contain organization update data.",
            };
        }

        var organizationQuery = _repository.Query(o => o.Id == request.Id && !o.IsDeleted);
        var organization = await _repository.FirstOrDefaultAsync(organizationQuery, cancellationToken);

        if (organization is null)
        {
            return new ApiResponse<UpdateOrganizationDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", new[] { "Organization not found." } }
                },
                Message = "The organization you are trying to edit does not exist.",
            };
        }

        var existingOrganizationWithNameQuery = _repository.Query(
            o => o.Name == request.Organization.Name && o.Id != request.Id && !o.IsDeleted);
        var existingOrganizationWithName = await _repository.FirstOrDefaultAsync(existingOrganizationWithNameQuery, cancellationToken);

        if (existingOrganizationWithName is not null)
        {
            return new ApiResponse<UpdateOrganizationDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Name", new[] { "An organization with the specified name already exists." } }
                },
                Message = "An organization with the specified name already exists.",
            };
        }

        organization.Name = request.Organization.Name;
        organization.Description = request.Organization.Description;

        _repository.Update(organization);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UpdateOrganizationDto>
        {
            Succeeded = true,
            Data = new UpdateOrganizationDto
            {
                Name = request.Organization.Name,
                Description = request.Organization.Description
            },
            Message = "Organization updated successfully.",
        };
    }
}
