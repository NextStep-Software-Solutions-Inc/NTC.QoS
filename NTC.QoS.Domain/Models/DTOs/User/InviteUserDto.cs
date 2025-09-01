namespace NTC.QoS.Domain.Models.DTOs.User;

public class InviteUserDto
{
    public required string Email { get; set; }
    public required Guid RoleId { get; set; }
    public required Guid OrganizationId { get; set; }
}