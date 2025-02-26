using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;

namespace NTC.QoS.Application.Features.Barangay.Requests;

public class UpdateBarangayByIdRequest : IRequest<ApiResponse<Domain.Entities.Barangay>>
{
    public UpdateBarangayByIdRequest(Guid id, UpdateBarangayDto barangay)
    {
        Id = id;
        Barangay = barangay;
    }
    public Guid Id { get; set; }
    public UpdateBarangayDto Barangay { get; set; }
}
