namespace NTC.QoS.Domain.Models.DTOs.User;

public class UpdateUserDto
{
    public Guid? RoleId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? AuthUserId { get; set; }
}
