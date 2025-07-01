using MediatR;
using NTC.QoS.Application.Features.NetworkTemplate.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.NetworkTemplate.Handlers.Commands;

public class CreateNetworkTemplateHanlder(IRepository<Domain.Entities.NetworkTemplate> repository) : IRequestHandler<CreateNetworkTemplateRequest, ApiResponse<NetworkTemplateDto>>
{
    private readonly IRepository<Domain.Entities.NetworkTemplate> _repository = repository;

    public async Task<ApiResponse<NetworkTemplateDto>> Handle(CreateNetworkTemplateRequest request, CancellationToken cancellationToken)
    {
        var networkTemplate = _repository.Query(nt => nt.Name == request.CreateNetworkTemplate.Name);
        var existingNetworkTemplate = await _repository.FirstOrDefaultAsync(networkTemplate, cancellationToken);

        if (existingNetworkTemplate is not null)
        {
            return new ApiResponse<NetworkTemplateDto>
            {
                Data = null,
                Message = "Network template already exist.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Name", ["Network template with the same name already exists."]   }
                }
            };
        }

        var newNetworkTemplate = new Domain.Entities.NetworkTemplate
        {
            Name = request.CreateNetworkTemplate.Name,
            Description = request.CreateNetworkTemplate.Description,
            URL = request.CreateNetworkTemplate.URL
        };

        _repository.Add(newNetworkTemplate);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<NetworkTemplateDto>
        {
            Succeeded = true,
            Message = "Network template created successfully.",
            Data = new NetworkTemplateDto
            {
                Id = newNetworkTemplate.Id,
                Name = newNetworkTemplate.Name,
                Description = newNetworkTemplate.Description,
                URL = newNetworkTemplate.URL
            }
        };
    }
}
