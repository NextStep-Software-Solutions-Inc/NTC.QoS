namespace NTC.QoS.Domain.Models.DTOs.User;

public class GetUserDto
{
    public Guid Id { get; set; }
    public RoleUserDto? Role { get; set; }
    public OrganizationUserDto? Organization { get; set; }
    public required string AuthUserId { get; set; }
}
