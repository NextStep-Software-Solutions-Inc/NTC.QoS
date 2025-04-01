using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;

namespace NTC.QoS.Application.Features.Barangay.Requests;

public class CreateBarangayRequest : IRequest<ApiResponse<BarangayDto>>
{
    public required CreateBarangayDto Barangay { get; set; }
}
