namespace NTC.QoS.Domain.Models.DTOs.Speedtest;

/// <summary>
/// Complete speedtest analytics dashboard response
/// Calculates ALL statistics on backend regardless of pagination
/// </summary>
public class SpeedtestAnalyticsDto
{
    public SpeedtestOverviewDto Overview { get; set; } = new();
    public List<ProviderPerformanceDto> ProviderPerformance { get; set; } = [];
    public SignalQualityDistributionDto SignalQualityDistribution { get; set; } = new();
    public List<ProviderRankingDto> ProviderRankings { get; set; } = [];
}

/// <summary>
/// Overview statistics for speedtest dashboard
/// </summary>
public class SpeedtestOverviewDto
{
    public int TotalTests { get; set; }
    public decimal AverageDownloadMbps { get; set; }
    public decimal AverageUploadMbps { get; set; }
    public decimal MedianDownloadMbps { get; set; }
    public decimal MedianUploadMbps { get; set; }
    public decimal AveragePingMs { get; set; }
    public int ProviderCount { get; set; }
    public double CoverageAreaKm2 { get; set; }
    
    /// <summary>
    /// Performance rating based on average download speed
    /// </summary>
    public string PerformanceRating { get; set; } = string.Empty;
    
    /// <summary>
    /// Best performing provider name
    /// </summary>
    public string BestProvider { get; set; } = string.Empty;
    
    /// <summary>
    /// Competition level: High, Medium, Low
    /// </summary>
    public string CompetitionLevel { get; set; } = string.Empty;
}

/// <summary>
/// Provider performance metrics
/// </summary>
public class ProviderPerformanceDto
{
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderColor { get; set; } = string.Empty;
    public decimal AverageDownloadMbps { get; set; }
    public decimal AverageUploadMbps { get; set; }
    public decimal MedianDownloadMbps { get; set; }
    public decimal MedianUploadMbps { get; set; }
    public int TestCount { get; set; }
}

/// <summary>
/// Signal quality distribution across all tests
/// </summary>
public class SignalQualityDistributionDto
{
    public int TotalTests { get; set; }
    public int ExcellentCount { get; set; }
    public int GoodCount { get; set; }
    public int AverageCount { get; set; }
    public int WeakCount { get; set; }
    public int VeryWeakCount { get; set; }
    public decimal ExcellentPercent { get; set; }
    public decimal GoodPercent { get; set; }
    public decimal AveragePercent { get; set; }
    public decimal WeakPercent { get; set; }
    public decimal VeryWeakPercent { get; set; }
}

/// <summary>
/// Provider ranking with score
/// </summary>
public class ProviderRankingDto
{
    public int Rank { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderColor { get; set; } = string.Empty;
    public int TestCount { get; set; }
    public decimal DownloadMbps { get; set; }
    public decimal UploadMbps { get; set; }
    public decimal AveragePingMs { get; set; }
    public int Score { get; set; }
    public string Badge { get; set; } = string.Empty;
}
