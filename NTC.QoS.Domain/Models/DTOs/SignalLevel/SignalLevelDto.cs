namespace NTC.QoS.Domain.Models.DTOs.SignalLevel;

public class SignalLevelDto
{
    public required Guid Id { get; set; }
    public required Guid AreaStudyId { get; set; }
    public required int NoOfLevels { get; set; }
    public List<SignalLevelEntryDto> SignalLevelEntries { get; set; } = [];
}