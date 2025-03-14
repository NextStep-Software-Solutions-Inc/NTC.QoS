namespace NTC.QoS.Domain.Models.DTOs.Organization;

public class UpdateOrganizationDto
{
    public required string Name { get; set; }
    public string? Description { get; set; } 
}
