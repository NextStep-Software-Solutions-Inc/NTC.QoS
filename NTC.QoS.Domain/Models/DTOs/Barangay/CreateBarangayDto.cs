using NTC.QoS.Domain.Models.DTOs.Municipality;

namespace NTC.QoS.Domain.Models.DTOs.Barangay;

public class CreateBarangayDto
{
    public Guid Id { get; set; }
    public Guid MunicipalityId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
