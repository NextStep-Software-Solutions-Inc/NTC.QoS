using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Municipality;

namespace NTC.QoS.Application.Features.Municipality.Requests;

public class UpdateMunicipalityByIdRequest(Guid id, UpdateMunicipalityDto municipality) : IRequest<ApiResponse<MunicipalityDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateMunicipalityDto Municipality { get; set; } = municipality;
}
