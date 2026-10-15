namespace NTC.QoS.Domain.Models.DTOs.SignalLevel;

public class CreateSignalLevelDto
{
    public required Guid AreaStudyId { get; set; }
    public required int NoOfLevels { get; set; }
}