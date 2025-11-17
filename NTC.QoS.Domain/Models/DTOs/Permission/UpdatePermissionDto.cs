namespace NTC.QoS.Domain.Models.DTOs.Permission;

public class UpdatePermissionDto
{
    public required string Name { get; set; }
    public required string Key { get; set; }
    public string? Description { get; set; }
}
