namespace NTC.QoS.Domain.Models.DTOs.Municipality;

public class UpdateMunicipalityDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsCity { get; set; }
    public string? Description { get; set; }
}
