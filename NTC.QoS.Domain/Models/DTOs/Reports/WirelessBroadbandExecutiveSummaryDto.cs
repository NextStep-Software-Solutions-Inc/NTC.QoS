using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Reports;

/// <summary>
/// Response DTO for wireless broadband executive summary report
/// Contains municipality-level breakdown with barangay wireless coverage details and connectivity scores
/// </summary>
public class WirelessBroadbandExecutiveSummaryDto
{
    public List<MunicipalityWirelessBroadbandSummaryDto> Municipalities { get; set; } = [];
    public WirelessBroadbandOverallSummaryDto Summary { get; set; } = new();
    
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

/// <summary>
/// Municipality-level wireless broadband summary data
/// </summary>
public class MunicipalityWirelessBroadbandSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public List<BarangayWirelessBroadbandSummaryDto> Barangays { get; set; } = [];
    public int TotalBarangays => Barangays.Count;
    public int ServedBarangays => Barangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Served);
    public int UnderservedBarangays => Barangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Underserved);
    public int UnservedBarangays => Barangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Unserved);
}

/// <summary>
/// Barangay-level wireless broadband summary data
/// </summary>
public class BarangayWirelessBroadbandSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Wireless technology availability by provider
    /// </summary>
    public WirelessProviderTechnologyAvailabilityDto ProviderAvailability { get; set; } = new();
    
    /// <summary>
    /// Overall wireless connectivity score (0-6 based on Technology Type and Internet Access)
    /// </summary>
    public int ConnectivityScore { get; set; }
    
    /// <summary>
    /// Service status determined from connectivity score
    /// </summary>
    public ServiceStatusEnum ServiceStatus { get; set; } = ServiceStatusEnum.Unserved;
    
    /// <summary>
    /// Highest wireless technology available in this barangay
    /// </summary>
    public string HighestTechnology { get; set; } = "None";
    
    /// <summary>
    /// Average download speed across all wireless providers (Mbps)
    /// </summary>
    public double? AverageDownloadSpeedMbps { get; set; }
    
    /// <summary>
    /// Average upload speed across all wireless providers (Mbps)
    /// </summary>
    public double? AverageUploadSpeedMbps { get; set; }
    
    /// <summary>
    /// Summary remarks about wireless coverage
    /// </summary>
    public string Remarks { get; set; } = string.Empty;
}

/// <summary>
/// Wireless provider availability with technology generation breakdown
/// </summary>
public class WirelessProviderTechnologyAvailabilityDto
{
    /// <summary>
    /// Dictionary of provider names and their wireless technology availability
    /// Key: Provider name, Value: Technology generations available (e.g., "4G, 5G")
    /// </summary>
    public Dictionary<string, WirelessTechnologyDetailsDto> Providers { get; set; } = new();
    
    /// <summary>
    /// List of provider names that have wireless coverage
    /// </summary>
    public List<string> AvailableProviders => Providers.Keys.ToList();
    
    public bool HasAnyProvider => Providers.Any();
    public int ProviderCount => Providers.Count;
}

/// <summary>
/// Wireless technology details for a provider
/// </summary>
public class WirelessTechnologyDetailsDto
{
    public bool Has2G { get; set; }
    public bool Has3G { get; set; }
    public bool Has4G { get; set; }
    public bool Has5G { get; set; }
    
    /// <summary>
    /// Highest generation available (e.g., "5G", "4G", "3G", "2G", or "None")
    /// </summary>
    public string HighestGeneration { get; set; } = "None";
    
    /// <summary>
    /// All available generations as comma-separated string (e.g., "3G, 4G, 5G")
    /// </summary>
    public string AvailableGenerations { get; set; } = string.Empty;
    
    /// <summary>
    /// Average download speed for this provider (Mbps)
    /// </summary>
    public double? AverageDownloadSpeedMbps { get; set; }
    
    /// <summary>
    /// Average upload speed for this provider (Mbps)
    /// </summary>
    public double? AverageUploadSpeedMbps { get; set; }
}

/// <summary>
/// Summary statistics for wireless broadband coverage
/// </summary>
public class WirelessBroadbandOverallSummaryDto
{
    public int TotalMunicipalities { get; set; }
    public int TotalBarangays { get; set; }
    public int ServedBarangays { get; set; }
    public int UnderservedBarangays { get; set; }
    public int UnservedBarangays { get; set; }
    
    public double ServedPercentage => TotalBarangays > 0 
        ? Math.Round((double)ServedBarangays / TotalBarangays * 100, 2) 
        : 0;
    
    public double UnderservedPercentage => TotalBarangays > 0 
        ? Math.Round((double)UnderservedBarangays / TotalBarangays * 100, 2) 
        : 0;
    
    public double UnservedPercentage => TotalBarangays > 0 
        ? Math.Round((double)UnservedBarangays / TotalBarangays * 100, 2) 
        : 0;
    
    /// <summary>
    /// List of all unique wireless providers found across all barangays
    /// </summary>
    public List<string> AllProviders { get; set; } = [];
    
    /// <summary>
    /// Provider coverage statistics - how many barangays each provider covers
    /// </summary>
    public Dictionary<string, int> ProviderCoverage { get; set; } = new();
    
    /// <summary>
    /// Technology generation distribution across all barangays
    /// </summary>
    public Dictionary<string, int> TechnologyDistribution { get; set; } = new();
    
    /// <summary>
    /// Average connectivity score across all barangays
    /// </summary>
    public double AverageConnectivityScore { get; set; }
    
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
