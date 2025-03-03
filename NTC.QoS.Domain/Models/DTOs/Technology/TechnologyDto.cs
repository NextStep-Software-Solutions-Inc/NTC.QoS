using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Domain.Models.DTOs.Technology;

public class TechnologyDto
{
    public Guid Id { get; set; }
    public string Standard { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid GenerationId { get; set; }
    public GenerationDto? Generation { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty; 
    public DateTime? ModifiedDate { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
}