using NTC.QoS.Domain.Models.DTOs.Region;
using NTC.QoS.Domain.Models.DTOs.Role;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Domain.Models.DTOs.UserRegion;

public class UserRegionDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? RoleId { get; set; }
    public RoleDto? Role { get; set; }
    public UserDto? User { get; set; }
    public RegionDto? Region { get; set; }
}
