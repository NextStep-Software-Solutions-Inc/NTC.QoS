namespace NTC.QoS.Domain.Models.DTOs.Municipality;

public class CreateMunicipalityDto
{
    public bool IsCity { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid ProvinceId { get; set; }
}
