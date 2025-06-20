using NTC.QoS.Domain.Models.DTOs.Province;

namespace NTC.QoS.Domain.Models.DTOs.Municipality;

public class MunicipalityDto
{
    public Guid Id { get; set; }
    public bool IsCity { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ProvinceDto? Province { get; set; }
    public string Code { get; set; } = string.Empty;
    public float Area { get; set; }
}
