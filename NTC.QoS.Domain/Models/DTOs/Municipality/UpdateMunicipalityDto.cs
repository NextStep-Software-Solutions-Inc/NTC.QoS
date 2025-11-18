using Microsoft.AspNetCore.Http;

namespace NTC.QoS.Domain.Models.DTOs.Municipality;

public class UpdateMunicipalityDto
{
    public string? Name { get; set; }
    public bool? IsCity { get; set; }
    public string? Description { get; set; }
    public string? Code { get; set; }
    public IFormFile? BoundariesFile { get; set; }
    public float? Area { get; set; }
}
