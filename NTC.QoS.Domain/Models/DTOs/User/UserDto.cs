using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Domain.Models.DTOs.User;

public class UserDto
{
    public Guid Id { get; set; }
    public RoleDto? Role { get; set; }
    public required string AuthUserId { get; set; } = string.Empty;
    public required string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
