namespace NTC.QoS.Domain.Models.DTOs.User;

public class RoleUserDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Key { get; set; }
    public string? Description { get; set; }
}
