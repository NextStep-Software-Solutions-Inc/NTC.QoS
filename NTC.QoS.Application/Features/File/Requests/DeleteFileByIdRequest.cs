using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.File.Requests;

public class DeleteFileByIdRequest : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public DeleteFileByIdRequest(Guid id)
    {
        Id = id;
    }
}