namespace NTC.QoS.Domain.Models.DTOs.StudyNetwork;

public class CreateStudyNetworkDto
{
    public required Guid StudyId { get; set; }
    public required Guid NetworkId { get; set; }
}
