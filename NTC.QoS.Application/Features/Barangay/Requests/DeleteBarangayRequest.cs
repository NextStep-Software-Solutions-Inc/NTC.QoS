using MediatR;
using NTC.QoS.Domain.Models.DTOs;
namespace NTC.QoS.Application.Features.Barangay.Requests;

public class DeleteBarangayByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteBarangayByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


