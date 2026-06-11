namespace NTC.QoS.Domain.Models.DTOs.SignalLevel;

public class UpdateSignalLevelDto
{
    public Guid? AreaStudyId { get; set; }
    public int? NoOfLevels { get; set; }
}