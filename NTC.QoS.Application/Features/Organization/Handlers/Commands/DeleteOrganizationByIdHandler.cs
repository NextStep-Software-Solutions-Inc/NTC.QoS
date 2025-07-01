using MediatR;
using NTC.QoS.Application.Features.Organization.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Organization.Handlers.Commands;

public class DeleteOrganizationByIdHandler(IRepository<Domain.Entities.Organization> repository) : IRequestHandler<DeleteOrganizationByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Organization> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteOrganizationByIdRequest request, CancellationToken cancellationToken)
    {
        var organizationQuery = _repository.Query(o => o.Id == request.Id);
        var organization = await _repository.FirstOrDefaultAsync(organizationQuery, cancellationToken);

        if (organization is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]> 
                { 
                    { "Error", ["Organization not found."] }
                },
                Message = "The organization you are trying to delete does not exist."
            };
        }

        _repository.Delete(organization);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = $"Organization deleted successfully."
        };
    }
}
