using NTC.QoS.Domain.Models.DTOs.Network;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Domain.Models.DTOs.StudyNetwork;

public class StudyNetworkDto
{
    public required Guid Id { get; set; }
    public StudyDto? Study { get; set; }
    public NetworkDto? Network { get; set; }
}
