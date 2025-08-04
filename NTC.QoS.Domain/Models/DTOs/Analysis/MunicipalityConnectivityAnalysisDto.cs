using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Analysis;

public class MunicipalityConnectivityAnalysisDto
{
    public Guid MunicipalityId { get; set; }
    public string MunicipalityName { get; set; } = string.Empty;
    public ServiceClassificationEnum Classification { get; set; }
    public decimal Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> DeficiencyAreas { get; set; } = new();
    public ConnectivityMetricsDto Metrics { get; set; } = new();
}

public class ConnectivityMetricsDto
{
    public decimal AverageDownloadSpeed { get; set; }
    public decimal AverageUploadSpeed { get; set; }
    public decimal AveragePing { get; set; }
    public decimal AverageJitter { get; set; }
    public decimal AverageRSSI { get; set; }
    public int SpeedCompliantTests { get; set; }
    public int TotalTests { get; set; }
    public decimal SpeedComplianceRate { get; set; }
    public int UniqueProviders { get; set; }
    public int UniqueCells { get; set; }
    public DateTime EarliestTest { get; set; }
    public DateTime LatestTest { get; set; }
    public Dictionary<SignalQualityEnum, int> SignalQualityDistribution { get; set; } = new();
}