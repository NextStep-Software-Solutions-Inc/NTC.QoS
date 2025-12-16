using Microsoft.AspNetCore.Http;

namespace NTC.QoS.Domain.Models.DTOs.CellularCoverage;

/// <summary>
/// Request DTO for uploading CellularCoverage V2 CSV file
/// User provides: NetworkId, ServiceProviderId, MunicipalityId
/// System derives: BandId and TechnologyId from Network
/// </summary>
public class UploadCellularCoverageV2Request
{
    /// <summary>
    /// Network identifier (user-selected, filtered by technology/band/location/serviceprovider on frontend)
    /// </summary>
    public required Guid NetworkId { get; set; }

    /// <summary>
    /// Service Provider identifier (user-selected)
    /// </summary>
    public required Guid ServiceProviderId { get; set; }

    /// <summary>
    /// Municipality identifier (defines geographic scope)
    /// </summary>
    public required Guid MunicipalityId { get; set; }

    /// <summary>
    /// Region identifier (for blob storage organization)
    /// </summary>
    public required Guid RegionId { get; set; }

    /// <summary>
    /// CSV file containing coverage data (Lat, Lng, Signal, Address)
    /// </summary>
    public required IFormFile File { get; set; }
}

/// <summary>
/// Response DTO for V2 upload operation
/// </summary>
public class CellularCoverageV2UploadResultDto
{
    public Guid JobId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Summary DTO for V2 processing job
/// </summary>
public class CellularCoverageV2ProcessingSummary
{
    public int TotalRows { get; set; }
    public int ProcessedSuccessfully { get; set; }
    public int Skipped { get; set; }
    public int Duplicates { get; set; }
    public int InvalidCoordinates { get; set; }
    public int NoCellSectorFound { get; set; }
    public List<string> Errors { get; set; } = new();
}
