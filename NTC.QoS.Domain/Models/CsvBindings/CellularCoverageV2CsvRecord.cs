namespace NTC.QoS.Domain.Models.CsvBindings;

/// <summary>
/// CSV record binding for CellularCoverage V2 import
/// Expected CSV format: Lat, Lng, Signal, Address
/// </summary>
public record CellularCoverageV2CsvRecord
{
    /// <summary>
    /// Latitude coordinate
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Signal strength measurement
    /// </summary>
    public decimal Signal { get; set; }

    /// <summary>
    /// Address or location description
    /// </summary>
    public string Address { get; set; } = string.Empty;
}
