namespace NTC.QoS.Domain.Models.DTOs.NAP;

public class UpdateNAPDto
{
    public Guid? ServiceProviderId { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
