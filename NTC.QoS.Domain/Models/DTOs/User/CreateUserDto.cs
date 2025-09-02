namespace NTC.QoS.Domain.Models.DTOs.User;

public class CreateUserDto
{
    public required Guid RoleId { get; set; }
    public required Guid OrganizationId { get; set; }
    public required string AuthUserId { get; set; }
    public string Email { get; set; } = string.Empty;
}
