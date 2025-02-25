using MediatR;
using NTC.QoS.Domain.Models.DTOs.Municipality;

namespace NTC.QoS.Application.Features.Municipality.Requests;

public class UpdateMunicipalityByIdRequest : IRequest<ApiResponse<Domain.Entities.Municipality>>
{
    public UpdateMunicipalityByIdRequest(Guid id, UpdateMunicipalityDto municipality)
    {
        Id = id;
        Municipality = municipality;
    }
    public Guid Id { get; set; }
    public UpdateMunicipalityDto Municipality { get; set; }
}
