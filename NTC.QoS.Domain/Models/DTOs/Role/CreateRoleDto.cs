namespace NTC.QoS.Domain.Models.DTOs.Role;

public class CreateRoleDto
{
    public required string Name { get; set; }
    public required string Key { get; set; }
    public string? Description { get; set; } 
}
