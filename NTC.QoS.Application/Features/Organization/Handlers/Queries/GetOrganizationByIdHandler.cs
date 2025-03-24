using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Organization.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Organization;

namespace NTC.QoS.Application.Features.Organization.Handlers.Queries;

public class GetOrganizationByIdHandler(IRepository<Domain.Entities.Organization> repository) : IRequestHandler<GetOrganizationByIdRequest, ApiResponse<GetOrganizationDto>>
{
    private readonly IRepository<Domain.Entities.Organization> _repository = repository;

    public async Task<ApiResponse<GetOrganizationDto>> Handle(GetOrganizationByIdRequest request, CancellationToken cancellationToken)
    {
        var organization = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (organization is null)
        {
            return new ApiResponse<GetOrganizationDto>
            {
                Succeeded = false,
                Message = "Organization not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Organization with the specified ID does not exist."] },
                },
            };
        }

        return new ApiResponse<GetOrganizationDto>
        {
            Succeeded = true,
            Message = "Organization retrieved successfully.",
            Data = new GetOrganizationDto
            {
                Id = organization.Id,
                Name = organization.Name,
                Description = organization.Description
            },
        };
    }
}
