using MediatR;
using NTC.QoS.Application.Features.Organization.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Organization;

namespace NTC.QoS.Application.Features.Organization.Handlers.Commands;

public class CreateOrganizationHandler(IRepository<Domain.Entities.Organization> repository) : IRequestHandler<CreateOrganizationRequest, ApiResponse<OrganizationDto>>
{
    private readonly IRepository<Domain.Entities.Organization> _repository = repository;

    public async Task<ApiResponse<OrganizationDto>> Handle(CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var organizationQueryAll = _repository.QueryWithIgnoreQueryFilters(o => o.Name == request.Organization.Name);
        var existing = await _repository.FirstOrDefaultAsync(organizationQueryAll, cancellationToken);

        if (existing is not null && existing.IsDeleted)
        {
            var restoreQuery = _repository.QueryWithIgnoreQueryFilters(o => o.Id == existing.Id && o.IsDeleted);
            await _repository.RestoreAsync(restoreQuery, cancellationToken);
            return new ApiResponse<OrganizationDto>
            {
                Data = new OrganizationDto
                {
                    Id = existing.Id,
                    Name = existing.Name,
                    Description = existing.Description,
                },
                Succeeded = true,
                Message = "Organization restored successfully."
            };
        }

        if (existing is not null)
        {
            return new ApiResponse<OrganizationDto>
            {
                Succeeded = false,
                Message = "Organization already exists.",
                Errors = new Dictionary<string, string[]>
                {
                    {"Duplicate", ["Organization with the same name already exists."]}
                }
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
