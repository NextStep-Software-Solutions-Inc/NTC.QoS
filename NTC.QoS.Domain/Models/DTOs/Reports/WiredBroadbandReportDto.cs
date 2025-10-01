namespace NTC.QoS.Domain.Models.DTOs.Reports;

public class WiredBroadbandReportDto
{
    public List<MunicipalityBroadbandDto> Municipalities { get; set; } = [];
    public WiredBroadbandSummaryDto Summary { get; set; } = new();
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
    public bool PLDT { get; set; }
    public bool Globe { get; set; }
    public bool Converge { get; set; }
    public bool Parasat { get; set; }
    public bool DCTech { get; set; }
    public bool SouthernCablevision { get; set; }
    public bool Others { get; set; }
    
    public bool HasAnyProvider => PLDT || Globe || Converge || Parasat || DCTech || SouthernCablevision || Others;
    public int ProviderCount => (PLDT ? 1 : 0) + (Globe ? 1 : 0) + (Converge ? 1 : 0) + 
                               (Parasat ? 1 : 0) + (DCTech ? 1 : 0) + (SouthernCablevision ? 1 : 0) + (Others ? 1 : 0);
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
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public enum ServiceStatusEnum
{
    Unserved = 0,
    Underserved = 1,
    Served = 2
}