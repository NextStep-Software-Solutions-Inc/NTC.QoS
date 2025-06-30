using Microsoft.AspNetCore.Http;

namespace NTC.QoS.Domain.Models.DTOs.Province;

public class UpdateProvinceDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Code { get; set; }
    public float? Area { get; set; }
    public IFormFile? BoundariesFile { get; set; }
}
