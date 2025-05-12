using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Domain.Models.DTOs.NAP;

public class NAPDto
{
    public Guid Id { get; set; }
    public required ServiceProviderDto ServiceProvider { get; set; }
    public string Address { get; set; } = string.Empty;
    public bool IsFileUploaded { get; set; } = false;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Distance { get; set; }
}
