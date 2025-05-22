namespace NTC.QoS.Domain.Models.DTOs.NAP;

// This DTO will directly bind to the result of your SQL query
public class ClusteredNapResultDto
{
    // Fields from your original NAPDto for the representative NAP
    public Guid Id { get; set; }
    public Guid ServiceProviderId { get; set; } // Maps to cn."ServiceProviderId" AS ServiceProviderId
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Distance { get; set; } // This is the distance to the cluster centroid

    // New fields from the clustering logic
    public int ClusterId { get; set; }
    public int PointCount { get; set; }
}