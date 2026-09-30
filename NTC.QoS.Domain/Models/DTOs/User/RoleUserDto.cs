namespace NTC.QoS.Domain.Models.DTOs.User;

public class RoleUserDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; } = string.Empty;
    public required string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
}
