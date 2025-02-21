namespace NTC.QoS.Domain.Models.DTOs.Service;

public class UpdateServiceDto
{
    public string Name { get; set; } = string.Empty;
    public required string Type { get; set; }
}
