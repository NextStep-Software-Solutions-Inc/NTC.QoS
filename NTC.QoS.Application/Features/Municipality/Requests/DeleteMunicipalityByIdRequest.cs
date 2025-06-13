using MediatR;
using NTC.QoS.Domain.Models.DTOs;
namespace NTC.QoS.Application.Features.Municipality.Requests;

public class DeleteMunicipalitiesByIdsRequest : IRequest<ApiResponse<bool>>
{
    public required List<Guid> Ids { get; set; }
}


