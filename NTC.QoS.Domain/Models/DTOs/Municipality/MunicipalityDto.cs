using NTC.QoS.Domain.Models.DTOs.Province;

namespace NTC.QoS.Domain.Models.DTOs.Municipality;

public class MunicipalityDto
{
    public Guid Id { get; set; }
    public Guid ProvinceId { get; set; }
    public bool IsCity { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProvinceDto? Province { get; set; }
}
