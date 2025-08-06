using NTC.QoS.Domain.Models.DTOs.Region;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Domain.Models.DTOs.UserRegion;

public class UserRegionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RegionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public UserDto? User { get; set; }
    public RegionDto? Region { get; set; }
}
