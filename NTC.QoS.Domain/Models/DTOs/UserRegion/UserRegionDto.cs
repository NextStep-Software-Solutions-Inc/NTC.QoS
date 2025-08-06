namespace NTC.QoS.Domain.Models.DTOs.UserRegion;

public class UserRegionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RegionId { get; set; }
    public string Status { get; set; } = string.Empty;
    
    // Navigation properties
    public User.UserDto? User { get; set; }
    public Region.RegionDto? Region { get; set; }
}
