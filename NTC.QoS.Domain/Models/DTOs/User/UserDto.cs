namespace NTC.QoS.Domain.Models.DTOs.User;

public class UserDto
{
    public Guid Id { get; set; }
    public required RoleUserDto Role { get; set; }
    public required OrganizationUserDto Organization { get; set; }
    public required string AuthUserId { get; set; }
}
