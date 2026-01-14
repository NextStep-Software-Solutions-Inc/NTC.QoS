using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Reports;

/// <summary>
/// Wireless broadband data report with municipalities and barangays (for API data response, not file download)
/// </summary>
public class WirelessBroadbandDataReportDto
{
    public List<MunicipalityWirelessBroadbandDto> Municipalities { get; set; } = [];
    public WirelessBroadbandScoredSummaryDto Summary { get; set; } = new();
    
    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// Number of municipalities per page
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Total number of municipalities matching the filters (across all pages)
    /// </summary>
    public int TotalMunicipalities { get; set; }
    
    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }
    
    /// <summary>
    /// Indicates if there are more pages available
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
    
    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
}

public class MunicipalityWirelessBroadbandDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public List<BarangayWirelessBroadbandDto> Barangays { get; set; } = [];
    public int TotalBarangays => Barangays.Count;
    public int ServedBarangays => Barangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Served);
    public int UnderservedBarangays => Barangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Underserved);
    public int UnservedBarangays => Barangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Unserved);
}

public class BarangayWirelessBroadbandDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Provider statistics (provider name -> stats) - using existing ProviderWirelessStatsDto from Report folder
    /// </summary>
    public Dictionary<string, NTC.QoS.Domain.Models.DTOs.Report.ProviderWirelessStatsDto> ProviderStats { get; set; } = new();
    
    /// <summary>
    /// Overall connectivity scores for the barangay (only if includeScores=true)
    /// </summary>
    public WirelessConnectivityScoreDto? ConnectivityScore { get; set; }
    
    /// <summary>
    /// Overall service status based on scores (only if includeScores=true)
    /// </summary>
    public ServiceStatusEnum ServiceStatus { get; set; }
    
    /// <summary>
    /// Remarks about connectivity status
    /// </summary>
    public string Remarks { get; set; } = string.Empty;
}
