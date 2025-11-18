namespace NTC.QoS.Domain.Models.DTOs.Role;

public class UpdateRoleDto
{
    public required string Name { get; set; }
    public string? Key { get; set; }
    public string? Description { get; set; }
}
