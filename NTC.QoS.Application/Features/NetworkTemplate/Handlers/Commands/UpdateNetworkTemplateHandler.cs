using MediatR;
using NTC.QoS.Application.Features.NetworkTemplate.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.NetworkTemplate.Handlers.Commands;

public class UpdateNetworkTemplateHandler(IRepository<Domain.Entities.NetworkTemplate> repository) : IRequestHandler<UpdateNetworkTemplateRequest, ApiResponse<NetworkTemplateDto>>
{
    private readonly IRepository<Domain.Entities.NetworkTemplate> _repository = repository;
    public async Task<ApiResponse<NetworkTemplateDto>> Handle(UpdateNetworkTemplateRequest request, CancellationToken cancellationToken)
    {
        var networkTemplateExist = await _repository.FindAsync(nt => nt.Id == request.Id && !nt.IsDeleted, cancellationToken);
        if (networkTemplateExist is null)
        {
            return new ApiResponse<NetworkTemplateDto>
            {
                Data = null,
                Message = $"Network Template Id [{request.Id}] not found.",
                Error = new[] { $"No Network Template found with Id [{request.Id}]. It may not exist or has been deleted." },
                Succeeded = false
            };
        }
        networkTemplateExist.Name = request.NetworkTemplate.Name;
        networkTemplateExist.Description = request.NetworkTemplate.Description;
        networkTemplateExist.URL = request.NetworkTemplate.URL;

        _repository.Update(networkTemplateExist);
        await _repository.SaveChangesAsync();
        return new ApiResponse<NetworkTemplateDto>
        {
            Data = new NetworkTemplateDto
            {
                Id = networkTemplateExist.Id,
                Name = networkTemplateExist.Name,
                Description = networkTemplateExist.Description,
                URL = networkTemplateExist.URL
            },
            Message = "Network template retrieved successfully.",
            Succeeded = true
        };
    }
}
