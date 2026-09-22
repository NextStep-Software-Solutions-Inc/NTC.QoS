using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Domain.Models.DTOs.User;

public class CurrentUserDto
{
    public Guid Id { get; set; }
    public required string AuthUserId { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public RoleDto? Role { get; set; }
    public List<string> Permissions { get; set; } = [];
    public List<Guid> AccessibleRegionIds { get; set; } = [];
    public bool IsSuperAdmin { get; set; }
    public bool IsPlatformUser { get; set; }
}
