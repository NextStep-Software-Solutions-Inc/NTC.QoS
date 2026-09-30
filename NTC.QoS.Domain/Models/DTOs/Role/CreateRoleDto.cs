namespace NTC.QoS.Domain.Models.DTOs.Role;

public class CreateRoleDto
{
    public required string Name { get; set; } = string.Empty;
    public required string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Scope { get; set; } = "Regional";
    public Guid? RegionId { get; set; }
    public List<string> Permissions { get; set; } = [];
}
