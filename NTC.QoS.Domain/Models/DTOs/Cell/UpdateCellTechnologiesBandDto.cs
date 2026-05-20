namespace NTC.QoS.Domain.Models.DTOs.Cell;

/// <summary>
/// DTO for updating a cell's supported technologies and bands.
/// Allows bulk management of technology-band combinations for a specific cell site.
/// Separate from cellsite metadata updates (name, address, coordinates, etc.).
/// </summary>
public class UpdateCellTechnologiesBandDto
{
    /// <summary>
    /// List of technology-band combinations to assign to this cell.
    /// Each combination represents a Cellular record linking the cell's sectors to a specific technology and band.
    /// </summary>
    public List<CellTechnologyBandInputDto> TechnologyBands { get; set; } = [];
}

/// <summary>
/// Represents a single technology-band combination to be assigned to a cell.
/// </summary>
public class CellTechnologyBandInputDto
{
    /// <summary>
    /// The Technology ID (e.g., 4G LTE, 5G NR).
    /// </summary>
    public Guid TechnologyId { get; set; }

    /// <summary>
    /// The Band ID (e.g., Band 3, Band 7).
    /// </summary>
    public Guid BandId { get; set; }

    /// <summary>
    /// Optional: Service Provider for this technology-band combination.
    /// If not specified, will use the cell's primary service provider.
    /// </summary>
    public Guid? ServiceProviderId { get; set; }
}
