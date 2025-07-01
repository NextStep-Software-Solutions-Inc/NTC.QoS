namespace NTC.QoS.Domain.Models.DTOs.Organization;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; } 
}
