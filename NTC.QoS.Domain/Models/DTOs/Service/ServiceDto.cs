namespace NTC.QoS.Domain.Models.DTOs.Service;

public class ServiceDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
}
