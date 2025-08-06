namespace NTC.QoS.Domain.Models.DTOs.UserRegion;

public class CreateUserRegionDto
{
    public Guid UserId { get; set; }
    public Guid RegionId { get; set; }
    public string Status { get; set; } = string.Empty;
}
