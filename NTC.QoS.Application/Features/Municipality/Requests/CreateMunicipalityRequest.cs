using MediatR;
using NTC.QoS.Domain.Models.DTOs.Municipality;

namespace NTC.QoS.Application.Features.Municipality.Requests;

public class CreateMunicipalityRequest : IRequest<ApiResponse<CreateMunicipalityDto>>
{
    public required CreateMunicipalityDto Municipality { get; set; }
}
