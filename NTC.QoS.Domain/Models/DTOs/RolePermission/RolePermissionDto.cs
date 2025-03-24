namespace NTC.QoS.Domain.Models.DTOs.RolePermission;

public class RolePermissionDto
{
    //Role or Permission details
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Key { get; set; }
    public string? Description { get; set; }
}
