using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.NetworkTemplate.Requests;

public class UpdateNetworkTemplateRequest : IRequest<ApiResponse<Domain.Entities.NetworkTemplate>>
{
    public UpdateNetworkTemplateDto NetworkTemplate { get; set; }
    public Guid Id { get; set; }
    public UpdateNetworkTemplateRequest(Guid id, UpdateNetworkTemplateDto networkTemplate)
    {
        NetworkTemplate = networkTemplate;
        Id = id;
    }
}
