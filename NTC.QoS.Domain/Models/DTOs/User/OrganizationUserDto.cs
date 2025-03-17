namespace NTC.QoS.Domain.Models.DTOs.User;

public class OrganizationUserDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
