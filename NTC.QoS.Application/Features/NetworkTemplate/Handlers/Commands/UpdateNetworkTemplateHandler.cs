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
        var networkTemplateExistQuery = _repository.Query(nt => nt.Id == request.Id);
        var networkTemplateExist = await _repository.FirstOrDefaultAsync(networkTemplateExistQuery, cancellationToken);

        if (networkTemplateExist is null)
        {
            return new ApiResponse<NetworkTemplateDto>
            {
                Data = null,
                Message = $"Network Template Id [{request.Id}] not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", [$"No Network Template found with Id [{request.Id}]. It may not exist or has been deleted."] }
                },
                Succeeded = false
            };
        }
        networkTemplateExist.Name = request.NetworkTemplate.Name ?? networkTemplateExist.Name;
        networkTemplateExist.Description = request.NetworkTemplate.Description ?? networkTemplateExist.Description;
        networkTemplateExist.URL = request.NetworkTemplate.URL ?? networkTemplateExist.URL;

        _repository.Update(networkTemplateExist);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<NetworkTemplateDto>
        {
            Data = new NetworkTemplateDto
            {
                Id = networkTemplateExist.Id,
                Name = networkTemplateExist.Name,
                Description = networkTemplateExist.Description,
                URL = networkTemplateExist.URL
            },
            Message = "Network template updated successfully.",
            Succeeded = true
        };
    }
}
