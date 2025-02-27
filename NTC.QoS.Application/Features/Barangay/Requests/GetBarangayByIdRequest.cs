using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;
namespace NTC.QoS.Application.Features.Barangay.Requests;

public class GetBarangayByIdRequest : IRequest<ApiResponse<BarangayDto>>
{
    public GetBarangayByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


