using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Requests;

public class GetFileByIdRequest : IRequest<ApiResponse<Domain.Entities.File>>
{
    public Guid Id { get; set; }
    public GetFileByIdRequest(Guid id)
    {
        Id = id;
    }
}