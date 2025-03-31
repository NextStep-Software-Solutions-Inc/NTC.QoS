namespace NTC.QoS.Domain.Models.DTOs.Municipality;

public class CreateMunicipalityDto
{
    public Guid ProvinceId { get; set; }
    public bool IsCity { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
