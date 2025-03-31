using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.StudyNetwork;

namespace NTC.QoS.Application.Features.StudyNetwork.Requests;

public class CreateStudyNetworkRequest(CreateStudyNetworkDto studyNetwork) : IRequest<ApiResponse<StudyNetworkDto>>
{
    public CreateStudyNetworkDto StudyNetwork { get; set; } = studyNetwork;
}
