namespace NTC.QoS.Domain.Models.DTOs.Reports;

public class WiredBroadbandReportDto
{
    public List<MunicipalityBroadbandDto> Municipalities { get; set; } = [];
    public WiredBroadbandSummaryDto Summary { get; set; } = new();
    
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

public class MunicipalityBroadbandDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public List<BarangayBroadbandDto> Barangays { get; set; } = [];
    public int TotalBarangays => Barangays.Count;
    public int ServedBarangays => Barangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Served);
    public int UnderservedBarangays => Barangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Underserved);
    public int UnservedBarangays => Barangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Unserved);
}

public class BarangayBroadbandDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public ServiceProviderAvailabilityDto ProviderAvailability { get; set; } = new();
    public ServiceStatusEnum ServiceStatus { get; set; } = ServiceStatusEnum.Unserved;
    public string Remarks { get; set; } = string.Empty;
}

public class ServiceProviderAvailabilityDto
{
    /// <summary>
    /// Dictionary of provider names and their availability status
    /// Key: Provider name, Value: Available (true/false)
    /// </summary>
    public Dictionary<string, bool> Providers { get; set; } = new();
    
    /// <summary>
    /// List of available provider names for easier access
    /// </summary>
    public List<string> AvailableProviders => Providers.Where(p => p.Value).Select(p => p.Key).ToList();
    
    public bool HasAnyProvider => Providers.Any(p => p.Value);
    public int ProviderCount => Providers.Count(p => p.Value);
}

public class WiredBroadbandSummaryDto
{
    public int TotalMunicipalities { get; set; }
    public int TotalBarangays { get; set; }
    public int ServedBarangays { get; set; }
    public int UnderservedBarangays { get; set; }
    public int UnservedBarangays { get; set; }
    public double ServedPercentage => TotalBarangays > 0 ? Math.Round((double)ServedBarangays / TotalBarangays * 100, 2) : 0;
    public double UnderservedPercentage => TotalBarangays > 0 ? Math.Round((double)UnderservedBarangays / TotalBarangays * 100, 2) : 0;
    public double UnservedPercentage => TotalBarangays > 0 ? Math.Round((double)UnservedBarangays / TotalBarangays * 100, 2) : 0;
    
    /// <summary>
    /// List of all unique providers found across all barangays
    /// </summary>
    public List<string> AllProviders { get; set; } = [];
    
    /// <summary>
    /// Provider statistics - how many barangays each provider serves
    /// </summary>
    public Dictionary<string, int> ProviderCoverage { get; set; } = new();
    
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public enum ServiceStatusEnum
{
    Unserved = 0,
    Underserved = 1,
    Served = 2
}

// Additional DTOs for connectivity gap reporting
public class ConnectivityGapReportDto
{
    public int TotalUnservedAreas { get; set; }
    public int TotalUnderservedAreas { get; set; }
    public int UnservedPopulation { get; set; }
    public int UnderservedPopulation { get; set; }
    public List<PriorityAreaDto> PriorityAreas { get; set; } = [];
    public DateTime GeneratedAt { get; set; }
}

public class PriorityAreaDto
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public NTC.QoS.Domain.Enums.ConnectivityStatusEnum ConnectivityStatus { get; set; }
    public int? Population { get; set; }
    public int TotalScore { get; set; }
    public DateTime LastAssessed { get; set; }
}

// Wireless broadband reporting DTOs
public class WirelessBroadbandReportDto
{
    public List<MunicipalityBroadbandDto> Municipalities { get; set; } = [];
    public WirelessBroadbandSummaryDto Summary { get; set; } = new();
}

public class WirelessBroadbandSummaryDto
{
    public int TotalMunicipalities { get; set; }
    public int TotalBarangays { get; set; }
    public int CellCount2G { get; set; }
    public int CellCount3G { get; set; }
    public int CellCount4G { get; set; }
    public int CellCount5G { get; set; }
    public DateTime GeneratedAt { get; set; }
}