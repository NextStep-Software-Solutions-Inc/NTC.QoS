using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.StudyNetwork;

namespace NTC.QoS.Application.Features.StudyNetwork.Requests;

public class GetStudyNetworkByIdRequest(Guid id) : IRequest<ApiResponse<StudyNetworkDto>>
{
    public Guid Id { get; set; } = id;
}
