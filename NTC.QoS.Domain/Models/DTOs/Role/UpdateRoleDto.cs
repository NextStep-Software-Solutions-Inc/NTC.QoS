namespace NTC.QoS.Domain.Models.DTOs.Role;

public class UpdateRoleDto
{
    public required string Name { get; set; }
    public string? Key { get; set; }
    public string? Description { get; set; }
    public string? Scope { get; set; }
    public Guid? RegionId { get; set; }
    public List<string>? Permissions { get; set; }
}
