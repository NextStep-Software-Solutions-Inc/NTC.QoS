namespace NTC.QoS.Domain.Models.DTOs.Permission;

public class PermissionDto
{
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public required string? Description { get; set; }
    public string Id => Key;
}
