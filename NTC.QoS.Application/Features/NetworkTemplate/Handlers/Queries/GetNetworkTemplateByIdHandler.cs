using MediatR;
using NTC.QoS.Application.Features.NetworkTemplate.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.NetworkTemplate.Handlers.Queries;

public class GetNetworkTemplateByIdHandler(IRepository<Domain.Entities.NetworkTemplate> repository) : IRequestHandler<GetNetworkTemplateByIdRequest, ApiResponse<NetworkTemplateDto>>
{
    private readonly IRepository<Domain.Entities.NetworkTemplate> _repository = repository;
    public async Task<ApiResponse<NetworkTemplateDto>> Handle(GetNetworkTemplateByIdRequest request, CancellationToken cancellationToken)
    {
        var networkTemplateExist = await _repository.FindAsync(nt => nt.Id == request.Id && !nt.IsDeleted, cancellationToken);
        if (networkTemplateExist == null)
        {
            return new ApiResponse<NetworkTemplateDto>
            {
                Succeeded = false,
                Message = $"Network Template Id [{request.Id}] not found.",
                Error = new[] { $"No Network Template found with Id [{request.Id}]. It may not exist or has been deleted." }
            };
        }
        var networkTemplate = new NetworkTemplateDto
        {
            Id = networkTemplateExist.Id,
            Name = networkTemplateExist.Name,
            URL = networkTemplateExist.URL,
            Description = networkTemplateExist.Description
        };
        return new ApiResponse<NetworkTemplateDto>
        {
            Data = networkTemplate,
            Message = "Retrieved successfully.",
            Succeeded = true
        };
    }
}
