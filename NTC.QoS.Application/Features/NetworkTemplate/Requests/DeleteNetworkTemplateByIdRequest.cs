using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.NetworkTemplate.Requests;

public class DeleteNetworkTemplateByIdRequest : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public DeleteNetworkTemplateByIdRequest(Guid id) 
    {
        Id = id;
    }
}