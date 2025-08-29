using NTC.QoS.Domain.Models.DTOs.Organization;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Domain.Models.DTOs.User;

public class UserDto
{
    public Guid Id { get; set; }
    public required RoleDto? Role { get; set; }
    public required OrganizationDto? Organization { get; set; }
    public required string AuthUserId { get; set; }
    public required string Email { get; set; }
}
