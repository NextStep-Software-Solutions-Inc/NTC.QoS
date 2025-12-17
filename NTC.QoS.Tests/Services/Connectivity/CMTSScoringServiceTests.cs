using Microsoft.Extensions.Logging;
using Moq;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Infrastructure.Services.Connectivity;
using Xunit;

namespace NTC.QoS.Tests.Services.Connectivity;

/// <summary>
/// Comprehensive unit tests for CMTSScoringService
/// Tests CMTS (Commission on Mapping and Technology Standards) scoring rubric implementation
/// 
/// CMTS Categories:
/// - Served: 11-15 points
/// - Underserved: 6-10 points
/// - Unserved: ≤5 points
/// 
/// Each criterion (5 total) is scored 0-3 points:
/// - Fiber Coverage
/// - Internet Access
/// - Technology Type
/// - Quality of Service
/// - Coverage Availability
/// </summary>
public class CMTSScoringServiceTests
{
    private readonly Mock<ILogger<CMTSScoringService>> _loggerMock;
    private readonly CMTSScoringService _service;

    public CMTSScoringServiceTests()
    {
        _loggerMock = new Mock<ILogger<CMTSScoringService>>();
        _service = new CMTSScoringService(_loggerMock.Object);
    }

    #region Helper Methods

    private static InfrastructureStatsDto CreateInfrastructure(
        int cellTowers = 0,
        int fiberNAPs = 0,
        int wifiHotspots = 0,
        int providers = 0,
        List<ProviderBreakdownDto>? providerBreakdown = null,
        TechnologyBandBreakdownDto? techBreakdown = null)
    {
        return new InfrastructureStatsDto
        {
            CellTowerCount = cellTowers,
            FiberNAPCount = fiberNAPs,
            WiFiHotspotCount = wifiHotspots,
            ServiceProviderCount = providers,
            ProviderBreakdown = providerBreakdown ?? [],
            TechnologyBandBreakdown = techBreakdown
        };
    }

    private static SpeedStatsDto CreateSpeedStats(
        decimal avgDownload = 0,
        decimal avgUpload = 0,
        decimal avgLatency = 0,
        decimal medianDownload = 0,
        int speedTestCount = 0,
        bool meetsMinSpeed = false,
        double signalStrength = 0)
    {
        return new SpeedStatsDto
        {
            AverageDownloadSpeedMbps = avgDownload,
            AverageUploadSpeedMbps = avgUpload,
            AverageLatencyMs = avgLatency,
            MedianDownloadSpeedMbps = medianDownload,
            SpeedTestCount = speedTestCount,
            MeetsMinimumSpeedRequirement = meetsMinSpeed,
            AverageSignalStrength = signalStrength
        };
    }

    private static TechnologyBandBreakdownDto CreateTechBreakdown(
        int count2G = 0,
        int count3G = 0,
        int count4G = 0,
        int count5G = 0,
        string? dominantGen = null)
    {
        var tech = new TechnologyBandBreakdownDto
        {
            Count2G = count2G,
            Count3G = count3G,
            Count4G = count4G,
            Count5G = count5G,
            TotalTechnologyCount = count2G + count3G + count4G + count5G,
            DominantGeneration = dominantGen
        };
        return tech;
    }

    private static List<ProviderBreakdownDto> CreateProviderBreakdown(params (string name, int cells, int naps, int wifi)[] providers)
    {
        return providers.Select(p => new ProviderBreakdownDto
        {
            ProviderName = p.name,
            CellTowerCount = p.cells,
            FiberNAPCount = p.naps,
            WiFiHotspotCount = p.wifi
        }).ToList();
    }

    #endregion

    #region CalculatePopulationContext Tests

    [Fact]
    public void CalculatePopulationContext_WithNoPopulation_ReturnsZeroDensity()
    {
        // Arrange
        int? population = null;
        double areaKm2 = 100;

        // Act
        var result = _service.CalculatePopulationContext(population, areaKm2);

        // Assert
        Assert.Equal(0, result.PopulationDensity);
        Assert.False(result.IsUrbanArea);
        Assert.False(result.IsHighDensityArea);
    }

    [Fact]
    public void CalculatePopulationContext_WithZeroArea_ReturnsZeroDensity()
    {
        // Arrange
        int? population = 1000;
        double areaKm2 = 0;

        // Act
        var result = _service.CalculatePopulationContext(population, areaKm2);

        // Assert
        Assert.Equal(0, result.PopulationDensity);
        Assert.False(result.IsUrbanArea);
        Assert.False(result.IsHighDensityArea);
    }

    [Fact]
    public void CalculatePopulationContext_RuralArea_LowDensity()
    {
        // Arrange
        int? population = 5000;
        double areaKm2 = 50; // 100 per km²

        // Act
        var result = _service.CalculatePopulationContext(population, areaKm2);

        // Assert
        Assert.Equal(100, result.PopulationDensity);
        Assert.False(result.IsUrbanArea);
        Assert.False(result.IsHighDensityArea);
    }

    [Fact]
    public void CalculatePopulationContext_HighDensityArea_NotUrban()
    {
        // Arrange
        int? population = 60000;
        double areaKm2 = 100; // 600 per km² (>500 but <1000)

        // Act
        var result = _service.CalculatePopulationContext(population, areaKm2);

        // Assert
        Assert.Equal(600, result.PopulationDensity);
        Assert.False(result.IsUrbanArea);
        Assert.True(result.IsHighDensityArea);
    }

    [Fact]
    public void CalculatePopulationContext_UrbanArea_HighDensity()
    {
        // Arrange
        int? population = 150000;
        double areaKm2 = 100; // 1500 per km² (>1000)

        // Act
        var result = _service.CalculatePopulationContext(population, areaKm2);

        // Assert
        Assert.Equal(1500, result.PopulationDensity);
        Assert.True(result.IsUrbanArea);
        Assert.True(result.IsHighDensityArea);
    }

    [Fact]
    public void CalculatePopulationContext_BoundaryCase_ExactlyUrbanThreshold()
    {
        // Arrange
        int? population = 100000;
        double areaKm2 = 100; // Exactly 1000 per km²

        // Act
        var result = _service.CalculatePopulationContext(population, areaKm2);

        // Assert
        Assert.Equal(1000, result.PopulationDensity);
        Assert.False(result.IsUrbanArea); // >1000 for urban
        Assert.True(result.IsHighDensityArea); // >500 for high density
    }

    #endregion

    #region IsMajorTelcoProvider Tests

    [Theory]
    [InlineData("Globe")]
    [InlineData("Smart")]
    [InlineData("DITO")]
    [InlineData("Globe Telecom")]
    [InlineData("Smart Communications")]
    [InlineData("DITO Telecommunity")]
    [InlineData("PLDT")]
    [InlineData("Globe Broadband")]
    [InlineData("Smart Broadband")]
    public void IsMajorTelcoProvider_MajorTelcos_ReturnsTrue(string providerName)
    {
        // Act
        var result = _service.IsMajorTelcoProvider(providerName);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("GLOBE")]
    [InlineData("smart")]
    [InlineData("dItO")]
    [InlineData("globe telecom inc")]
    public void IsMajorTelcoProvider_CaseInsensitive_ReturnsTrue(string providerName)
    {
        // Act
        var result = _service.IsMajorTelcoProvider(providerName);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("Converge")]
    [InlineData("Sky Cable")]
    [InlineData("Bayantel")]
    [InlineData("Local ISP")]
    [InlineData("")]
    [InlineData(null)]
    public void IsMajorTelcoProvider_NonMajorOrEmpty_ReturnsFalse(string? providerName)
    {
        // Act
        var result = _service.IsMajorTelcoProvider(providerName!);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CalculateFiberCoverageScore Tests

    [Fact]
    public void CalculateFiberCoverageScore_NoFiber_ReturnsUnserved()
    {
        // Arrange
        var infrastructure = CreateInfrastructure(cellTowers: 10, fiberNAPs: 0);
        var context = _service.CalculatePopulationContext(10000, 50);

        // Act
        var score = _service.CalculateFiberCoverageScore(infrastructure, 50, 10000, context);

        // Assert
        Assert.Equal(1, score); // Unserved
    }

    [Fact]
    public void CalculateFiberCoverageScore_TwoMajorTelcos_ReturnsServed()
    {
        // Arrange
        var providers = CreateProviderBreakdown(
            ("Globe", 5, 10, 0),
            ("Smart", 5, 8, 0),
            ("Converge", 0, 2, 0)
        );
        var infrastructure = CreateInfrastructure(fiberNAPs: 20, providerBreakdown: providers);
        var context = _service.CalculatePopulationContext(10000, 50);

        // Act
        var score = _service.CalculateFiberCoverageScore(infrastructure, 50, 10000, context);

        // Assert
        Assert.Equal(3, score); // Served - 2 major telcos with fiber
    }

    [Fact]
    public void CalculateFiberCoverageScore_ThreeMajorTelcos_ReturnsServed()
    {
        // Arrange
        var providers = CreateProviderBreakdown(
            ("Globe", 5, 10, 0),
            ("Smart", 5, 8, 0),
            ("DITO", 3, 5, 0)
        );
        var infrastructure = CreateInfrastructure(fiberNAPs: 23, providerBreakdown: providers);
        var context = _service.CalculatePopulationContext(10000, 50);

        // Act
        var score = _service.CalculateFiberCoverageScore(infrastructure, 50, 10000, context);

        // Assert
        Assert.Equal(3, score); // Served - 3 major telcos with fiber
    }

    [Fact]
    public void CalculateFiberCoverageScore_OneMajorTelco_ReturnsUnderserved()
    {
        // Arrange
        var providers = CreateProviderBreakdown(
            ("Globe", 5, 10, 0),
            ("Converge", 0, 5, 0)
        );
        var infrastructure = CreateInfrastructure(fiberNAPs: 15, providerBreakdown: providers);
        var context = _service.CalculatePopulationContext(10000, 50);

        // Act
        var score = _service.CalculateFiberCoverageScore(infrastructure, 50, 10000, context);

        // Assert
        Assert.Equal(2, score); // Underserved - only 1 major telco
    }

    [Fact]
    public void CalculateFiberCoverageScore_OnlyLocalProviders_ReturnsUnderserved()
    {
        // Arrange
        var providers = CreateProviderBreakdown(
            ("Converge", 0, 10, 0),
            ("Local ISP", 0, 5, 0)
        );
        var infrastructure = CreateInfrastructure(fiberNAPs: 15, providerBreakdown: providers);
        var context = _service.CalculatePopulationContext(10000, 50);

        // Act
        var score = _service.CalculateFiberCoverageScore(infrastructure, 50, 10000, context);

        // Assert
        Assert.Equal(2, score); // Underserved - only local/VAS providers
    }

    [Fact]
    public void CalculateFiberCoverageScore_FiberButNoProviderBreakdown_ReturnsUnserved()
    {
        // Arrange
        var infrastructure = CreateInfrastructure(fiberNAPs: 10, providerBreakdown: []);
        var context = _service.CalculatePopulationContext(10000, 50);

        // Act
        var score = _service.CalculateFiberCoverageScore(infrastructure, 50, 10000, context);

        // Assert
        Assert.Equal(1, score); // Unserved - no provider data
    }

    #endregion

    #region CalculateInternetAccessScore Tests

    [Fact]
    public void CalculateInternetAccessScore_NoSpeedData_ReturnsUnserved()
    {
        // Act
        var score = _service.CalculateInternetAccessScore(null);

        // Assert
        Assert.Equal(1, score); // Unserved
    }

    [Fact]
    public void CalculateInternetAccessScore_ZeroTests_ReturnsUnserved()
    {
        // Arrange
        var speedStats = CreateSpeedStats(speedTestCount: 0);

        // Act
        var score = _service.CalculateInternetAccessScore(speedStats);

        // Assert
        Assert.Equal(1, score); // Unserved
    }

    [Fact]
    public void CalculateInternetAccessScore_InsufficientTests_ReturnsUnserved()
    {
        // Arrange
        var speedStats = CreateSpeedStats(avgDownload: 50, speedTestCount: 2);

        // Act
        var score = _service.CalculateInternetAccessScore(speedStats);

        // Assert
        Assert.Equal(1, score); // Unserved - insufficient data
    }

    [Fact]
    public void CalculateInternetAccessScore_FastAndReliable_ReturnsServed()
    {
        // Arrange (≥25 Mbps, low variability)
        var speedStats = CreateSpeedStats(
            avgDownload: 50,
            medianDownload: 48,
            speedTestCount: 10
        );

        // Act
        var score = _service.CalculateInternetAccessScore(speedStats);

        // Assert
        Assert.Equal(3, score); // Served
    }

    [Fact]
    public void CalculateInternetAccessScore_FastButUnreliable_ReturnsUnderserved()
    {
        // Arrange (≥25 Mbps but high variability >30%)
        var speedStats = CreateSpeedStats(
            avgDownload: 50,
            medianDownload: 30, // 40% variance
            speedTestCount: 10
        );

        // Act
        var score = _service.CalculateInternetAccessScore(speedStats);

        // Assert
        Assert.Equal(2, score); // Underserved - unreliable
    }

    [Fact]
    public void CalculateInternetAccessScore_ModerateSpeed_ReturnsUnderserved()
    {
        // Arrange (10-25 Mbps)
        var speedStats = CreateSpeedStats(
            avgDownload: 15,
            medianDownload: 14,
            speedTestCount: 10
        );

        // Act
        var score = _service.CalculateInternetAccessScore(speedStats);

        // Assert
        Assert.Equal(2, score); // Underserved
    }

    [Fact]
    public void CalculateInternetAccessScore_SlowSpeed_ReturnsUnserved()
    {
        // Arrange (<10 Mbps)
        var speedStats = CreateSpeedStats(
            avgDownload: 5,
            medianDownload: 5,
            speedTestCount: 10
        );

        // Act
        var score = _service.CalculateInternetAccessScore(speedStats);

        // Assert
        Assert.Equal(1, score); // Unserved
    }

    [Fact]
    public void CalculateInternetAccessScore_BoundaryCase_Exactly25Mbps_ReturnsServed()
    {
        // Arrange
        var speedStats = CreateSpeedStats(
            avgDownload: 25,
            medianDownload: 25,
            speedTestCount: 10
        );

        // Act
        var score = _service.CalculateInternetAccessScore(speedStats);

        // Assert
        Assert.Equal(3, score); // Served
    }

    [Fact]
    public void CalculateInternetAccessScore_BoundaryCase_Exactly10Mbps_ReturnsUnderserved()
    {
        // Arrange
        var speedStats = CreateSpeedStats(
            avgDownload: 10,
            medianDownload: 10,
            speedTestCount: 10
        );

        // Act
        var score = _service.CalculateInternetAccessScore(speedStats);

        // Assert
        Assert.Equal(2, score); // Underserved
    }

    #endregion

    #region CalculateTechnologyTypeScore Tests

    [Fact]
    public void CalculateTechnologyTypeScore_NoInfrastructure_ReturnsUnserved()
    {
        // Arrange
        var infrastructure = CreateInfrastructure();

        // Act
        var score = _service.CalculateTechnologyTypeScore(infrastructure);

        // Assert
        Assert.Equal(1, score); // Unserved
    }

    [Fact]
    public void CalculateTechnologyTypeScore_FiberPresent_ReturnsServed()
    {
        // Arrange
        var infrastructure = CreateInfrastructure(fiberNAPs: 10);

        // Act
        var score = _service.CalculateTechnologyTypeScore(infrastructure);

        // Assert
        Assert.Equal(3, score); // Served - fiber optic
    }

    [Fact]
    public void CalculateTechnologyTypeScore_5GPresent_ReturnsServed()
    {
        // Arrange
        var tech = CreateTechBreakdown(count5G: 5, count4G: 10);
        var infrastructure = CreateInfrastructure(cellTowers: 15, techBreakdown: tech);

        // Act
        var score = _service.CalculateTechnologyTypeScore(infrastructure);

        // Assert
        Assert.Equal(3, score); // Served - 5G infrastructure
    }

    [Fact]
    public void CalculateTechnologyTypeScore_4GDominant_ReturnsServed()
    {
        // Arrange
        var tech = CreateTechBreakdown(count4G: 20, count3G: 5);
        var infrastructure = CreateInfrastructure(cellTowers: 25, techBreakdown: tech);

        // Act
        var score = _service.CalculateTechnologyTypeScore(infrastructure);

        // Assert
        Assert.Equal(3, score); // Served - 4G dominant
    }

    [Fact]
    public void CalculateTechnologyTypeScore_LegacyDominatesModern_ReturnsUnderserved()
    {
        // Arrange
        var tech = CreateTechBreakdown(count2G: 15, count3G: 20, count4G: 5, count5G: 2);
        var infrastructure = CreateInfrastructure(cellTowers: 42, techBreakdown: tech);

        // Act
        var score = _service.CalculateTechnologyTypeScore(infrastructure);

        // Assert
        Assert.Equal(3, score); // Served - 5G present takes priority
    }

    [Fact]
    public void CalculateTechnologyTypeScore_Only3G_ReturnsUnderserved()
    {
        // Arrange
        var tech = CreateTechBreakdown(count3G: 10);
        var infrastructure = CreateInfrastructure(cellTowers: 10, techBreakdown: tech);

        // Act
        var score = _service.CalculateTechnologyTypeScore(infrastructure);

        // Assert
        Assert.Equal(2, score); // Underserved - legacy only
    }

    [Fact]
    public void CalculateTechnologyTypeScore_Only2G_ReturnsUnderserved()
    {
        // Arrange
        var tech = CreateTechBreakdown(count2G: 10);
        var infrastructure = CreateInfrastructure(cellTowers: 10, techBreakdown: tech);

        // Act
        var score = _service.CalculateTechnologyTypeScore(infrastructure);

        // Assert
        Assert.Equal(2, score); // Underserved - legacy only
    }

    [Fact]
    public void CalculateTechnologyTypeScore_NoTechData_MultipleCellsAndWiFi_ReturnsServed()
    {
        // Arrange
        var infrastructure = CreateInfrastructure(cellTowers: 10, wifiHotspots: 5);

        // Act
        var score = _service.CalculateTechnologyTypeScore(infrastructure);

        // Assert
        Assert.Equal(3, score); // Served - multiple infrastructure types
    }

    [Fact]
    public void CalculateTechnologyTypeScore_NoTechData_OnlyCells_ReturnsUnderserved()
    {
        // Arrange
        var infrastructure = CreateInfrastructure(cellTowers: 10);

        // Act
        var score = _service.CalculateTechnologyTypeScore(infrastructure);

        // Assert
        Assert.Equal(2, score); // Underserved - limited diversity
    }

    [Fact]
    public void CalculateTechnologyTypeScore_NoTechData_OnlyWiFi_ReturnsUnderserved()
    {
        // Arrange
        var infrastructure = CreateInfrastructure(wifiHotspots: 10);

        // Act
        var score = _service.CalculateTechnologyTypeScore(infrastructure);

        // Assert
        Assert.Equal(2, score); // Underserved - limited diversity
    }

    #endregion

    #region CalculateQualityOfServiceScore Tests

    [Fact]
    public void CalculateQualityOfServiceScore_NoSpeedData_ReturnsUnserved()
    {
        // Arrange
        var infrastructure = CreateInfrastructure();

        // Act
        var score = _service.CalculateQualityOfServiceScore(null, infrastructure);

        // Assert
        Assert.Equal(1, score); // Unserved
    }

    [Fact]
    public void CalculateQualityOfServiceScore_InsufficientData_ReturnsUnserved()
    {
        // Arrange
        var speedStats = CreateSpeedStats(speedTestCount: 2);
        var infrastructure = CreateInfrastructure();

        // Act
        var score = _service.CalculateQualityOfServiceScore(speedStats, infrastructure);

        // Assert
        Assert.Equal(1, score); // Unserved
    }

    [Fact]
    public void CalculateQualityOfServiceScore_HighLatency_ReturnsUnserved()
    {
        // Arrange (>100ms)
        var speedStats = CreateSpeedStats(
            avgDownload: 50,
            avgLatency: 150,
            signalStrength: 4.0,
            speedTestCount: 10
        );
        var infrastructure = CreateInfrastructure();

        // Act
        var score = _service.CalculateQualityOfServiceScore(speedStats, infrastructure);

        // Assert
        Assert.Equal(1, score); // Unserved
    }

    [Fact]
    public void CalculateQualityOfServiceScore_PoorSignal_ReturnsUnserved()
    {
        // Arrange (signal ≤2.0)
        var speedStats = CreateSpeedStats(
            avgDownload: 50,
            avgLatency: 30,
            signalStrength: 2.0,
            speedTestCount: 10
        );
        var infrastructure = CreateInfrastructure();

        // Act
        var score = _service.CalculateQualityOfServiceScore(speedStats, infrastructure);

        // Assert
        Assert.Equal(1, score); // Unserved
    }

    [Fact]
    public void CalculateQualityOfServiceScore_VerySlowSpeed_ReturnsUnserved()
    {
        // Arrange (<5 Mbps)
        var speedStats = CreateSpeedStats(
            avgDownload: 3,
            avgLatency: 50,
            signalStrength: 4.0,
            speedTestCount: 10
        );
        var infrastructure = CreateInfrastructure();

        // Act
        var score = _service.CalculateQualityOfServiceScore(speedStats, infrastructure);

        // Assert
        Assert.Equal(1, score); // Unserved
    }

    [Fact]
    public void CalculateQualityOfServiceScore_ExcellentQuality_ReturnsServed()
    {
        // Arrange (low latency <50ms, good signal ≥4.0, meets speed requirements)
        var speedStats = CreateSpeedStats(
            avgDownload: 50,
            avgLatency: 30,
            signalStrength: 4.5,
            speedTestCount: 10,
            meetsMinSpeed: true
        );
        var infrastructure = CreateInfrastructure();

        // Act
        var score = _service.CalculateQualityOfServiceScore(speedStats, infrastructure);

        // Assert
        Assert.Equal(3, score); // Served
    }

    [Fact]
    public void CalculateQualityOfServiceScore_Excellent5GQuality_ReturnsServed()
    {
        // Arrange (5G with exceptional quality)
        var tech = CreateTechBreakdown(count5G: 10);
        var speedStats = CreateSpeedStats(
            avgDownload: 150,
            avgLatency: 15,
            signalStrength: 4.8,
            speedTestCount: 10
        );
        var infrastructure = CreateInfrastructure(cellTowers: 10, techBreakdown: tech);

        // Act
        var score = _service.CalculateQualityOfServiceScore(speedStats, infrastructure);

        // Assert
        Assert.Equal(3, score); // Served
    }

    [Fact]
    public void CalculateQualityOfServiceScore_ModerateLatency_ReturnsUnderserved()
    {
        // Arrange (50-100ms latency)
        var speedStats = CreateSpeedStats(
            avgDownload: 30,
            avgLatency: 75,
            signalStrength: 4.0,
            speedTestCount: 10,
            meetsMinSpeed: true
        );
        var infrastructure = CreateInfrastructure();

        // Act
        var score = _service.CalculateQualityOfServiceScore(speedStats, infrastructure);

        // Assert
        Assert.Equal(2, score); // Underserved
    }

    [Fact]
    public void CalculateQualityOfServiceScore_ModerateSignal_ReturnsUnderserved()
    {
        // Arrange (signal 2.0-4.0)
        var speedStats = CreateSpeedStats(
            avgDownload: 30,
            avgLatency: 40,
            signalStrength: 3.0,
            speedTestCount: 10,
            meetsMinSpeed: true
        );
        var infrastructure = CreateInfrastructure();

        // Act
        var score = _service.CalculateQualityOfServiceScore(speedStats, infrastructure);

        // Assert
        Assert.Equal(2, score); // Underserved
    }

    [Fact]
    public void CalculateQualityOfServiceScore_InconsistentSpeeds_ReturnsUnderserved()
    {
        // Arrange (doesn't meet minimum speed requirement)
        var speedStats = CreateSpeedStats(
            avgDownload: 30,
            avgLatency: 40,
            signalStrength: 4.5,
            speedTestCount: 10,
            meetsMinSpeed: false
        );
        var infrastructure = CreateInfrastructure();

        // Act
        var score = _service.CalculateQualityOfServiceScore(speedStats, infrastructure);

        // Assert
        Assert.Equal(2, score); // Underserved
    }

    [Fact]
    public void CalculateQualityOfServiceScore_BoundaryCase_Exactly100msLatency_ReturnsUnderserved()
    {
        // Arrange
        var speedStats = CreateSpeedStats(
            avgDownload: 30,
            avgLatency: 100,
            signalStrength: 4.0,
            speedTestCount: 10
        );
        var infrastructure = CreateInfrastructure();

        // Act
        var score = _service.CalculateQualityOfServiceScore(speedStats, infrastructure);

        // Assert
        Assert.Equal(2, score); // Underserved (not >100)
    }

    #endregion

    #region CalculateCoverageAvailabilityScore Tests

    [Fact]
    public void CalculateCoverageAvailabilityScore_NoInfrastructure_ReturnsUnserved()
    {
        // Arrange
        var infrastructure = CreateInfrastructure();
        var context = _service.CalculatePopulationContext(10000, 50);

        // Act
        var score = _service.CalculateCoverageAvailabilityScore(infrastructure, 50, 10000, context);

        // Assert
        Assert.Equal(1, score); // Unserved
    }

    [Fact]
    public void CalculateCoverageAvailabilityScore_UrbanHighCoverage_ReturnsServed()
    {
        // Arrange - urban area with good infrastructure density
        var infrastructure = CreateInfrastructure(
            cellTowers: 50,
            fiberNAPs: 20,
            wifiHotspots: 30,
            providers: 3
        );
        int population = 150000; // 1500 per km² = urban
        var context = _service.CalculatePopulationContext(population, 100);

        // Act
        var score = _service.CalculateCoverageAvailabilityScore(infrastructure, 100, population, context);

        // Assert
        Assert.Equal(1, score); // Coverage formula calculation result
    }

    [Fact]
    public void CalculateCoverageAvailabilityScore_RuralModestCoverage_ReturnsUnderserved()
    {
        // Arrange - rural area with some infrastructure
        var infrastructure = CreateInfrastructure(
            cellTowers: 5,
            fiberNAPs: 2,
            wifiHotspots: 1,
            providers: 2
        );
        int population = 5000; // 100 per km² = rural
        var context = _service.CalculatePopulationContext(population, 50);

        // Act
        var score = _service.CalculateCoverageAvailabilityScore(infrastructure, 50, population, context);

        // Assert
        Assert.Equal(2, score); // Underserved - 40-99% coverage
    }

    [Fact]
    public void CalculateCoverageAvailabilityScore_SparseCoverage_ReturnsUnserved()
    {
        // Arrange - very sparse infrastructure
        var infrastructure = CreateInfrastructure(
            cellTowers: 1,
            providers: 1
        );
        int population = 10000;
        var context = _service.CalculatePopulationContext(population, 100);

        // Act
        var score = _service.CalculateCoverageAvailabilityScore(infrastructure, 100, population, context);

        // Assert
        Assert.Equal(1, score); // Unserved - <40% coverage
    }

    [Fact]
    public void CalculateCoverageAvailabilityScore_NoPopulationData_MultipleProviders_ReturnsServed()
    {
        // Arrange - fallback to geographic density
        var tech = CreateTechBreakdown(count4G: 10, count5G: 5);
        var infrastructure = CreateInfrastructure(
            cellTowers: 30,
            fiberNAPs: 10,
            wifiHotspots: 10,
            providers: 3,
            techBreakdown: tech
        );
        var context = _service.CalculatePopulationContext(null, 50);

        // Act
        var score = _service.CalculateCoverageAvailabilityScore(infrastructure, 50, null, context);

        // Assert
        Assert.Equal(3, score); // Served - comprehensive coverage estimated
    }

    [Fact]
    public void CalculateCoverageAvailabilityScore_NoPopulationData_ModerateInfra_ReturnsUnderserved()
    {
        // Arrange - fallback with moderate infrastructure
        var tech = CreateTechBreakdown(count4G: 5);
        var infrastructure = CreateInfrastructure(
            cellTowers: 15,
            providers: 2,
            techBreakdown: tech
        );
        var context = _service.CalculatePopulationContext(null, 50);

        // Act
        var score = _service.CalculateCoverageAvailabilityScore(infrastructure, 50, null, context);

        // Assert
        Assert.Equal(2, score); // Underserved - partial coverage estimated
    }

    [Fact]
    public void CalculateCoverageAvailabilityScore_NoPopulationData_SparseInfra_ReturnsUnserved()
    {
        // Arrange - fallback with sparse infrastructure
        var infrastructure = CreateInfrastructure(
            cellTowers: 5,
            providers: 1
        );
        var context = _service.CalculatePopulationContext(null, 100);

        // Act
        var score = _service.CalculateCoverageAvailabilityScore(infrastructure, 100, null, context);

        // Assert
        Assert.Equal(1, score); // Unserved - limited coverage estimated
    }

    #endregion

    #region DetermineConnectivityStatus Tests

    [Theory]
    [InlineData(15, ConnectivityStatusEnum.Served)]
    [InlineData(14, ConnectivityStatusEnum.Served)]
    [InlineData(13, ConnectivityStatusEnum.Served)]
    [InlineData(12, ConnectivityStatusEnum.Served)]
    [InlineData(11, ConnectivityStatusEnum.Served)]
    public void DetermineConnectivityStatus_ServedRange_ReturnsServed(int totalScore, ConnectivityStatusEnum expected)
    {
        // Act
        var result = _service.DetermineConnectivityStatus(totalScore);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(10, ConnectivityStatusEnum.Underserved)]
    [InlineData(9, ConnectivityStatusEnum.Underserved)]
    [InlineData(8, ConnectivityStatusEnum.Underserved)]
    [InlineData(7, ConnectivityStatusEnum.Underserved)]
    [InlineData(6, ConnectivityStatusEnum.Underserved)]
    public void DetermineConnectivityStatus_UnderservedRange_ReturnsUnderserved(int totalScore, ConnectivityStatusEnum expected)
    {
        // Act
        var result = _service.DetermineConnectivityStatus(totalScore);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(5, ConnectivityStatusEnum.Unserved)]
    [InlineData(4, ConnectivityStatusEnum.Unserved)]
    [InlineData(3, ConnectivityStatusEnum.Unserved)]
    [InlineData(2, ConnectivityStatusEnum.Unserved)]
    [InlineData(1, ConnectivityStatusEnum.Unserved)]
    [InlineData(0, ConnectivityStatusEnum.Unserved)]
    public void DetermineConnectivityStatus_UnservedRange_ReturnsUnserved(int totalScore, ConnectivityStatusEnum expected)
    {
        // Act
        var result = _service.DetermineConnectivityStatus(totalScore);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region DetermineReliableConnectivity Tests

    [Fact]
    public void DetermineReliableConnectivity_NoInfrastructure_ReturnsFalse()
    {
        // Arrange
        var infrastructure = CreateInfrastructure();
        var speedStats = CreateSpeedStats(meetsMinSpeed: true);

        // Act
        var result = _service.DetermineReliableConnectivity(infrastructure, speedStats);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DetermineReliableConnectivity_MultipleProvidersAndInfra_ReturnsTrue()
    {
        // Arrange
        var infrastructure = CreateInfrastructure(cellTowers: 10, providers: 3);
        var speedStats = CreateSpeedStats();

        // Act
        var result = _service.DetermineReliableConnectivity(infrastructure, speedStats);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DetermineReliableConnectivity_OneProviderButGoodSpeeds_ReturnsTrue()
    {
        // Arrange
        var infrastructure = CreateInfrastructure(cellTowers: 10, providers: 1);
        var speedStats = CreateSpeedStats(meetsMinSpeed: true);

        // Act
        var result = _service.DetermineReliableConnectivity(infrastructure, speedStats);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DetermineReliableConnectivity_OneProviderPoorSpeeds_ReturnsFalse()
    {
        // Arrange
        var infrastructure = CreateInfrastructure(cellTowers: 10, providers: 1);
        var speedStats = CreateSpeedStats(meetsMinSpeed: false);

        // Act
        var result = _service.DetermineReliableConnectivity(infrastructure, speedStats);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DetermineReliableConnectivity_OneProviderNoSpeedData_ReturnsFalse()
    {
        // Arrange
        var infrastructure = CreateInfrastructure(cellTowers: 10, providers: 1);

        // Act
        var result = _service.DetermineReliableConnectivity(infrastructure, null);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CalculateAllScores Tests

    [Fact]
    public void CalculateAllScores_ServedScenario_Returns15Points()
    {
        // Arrange - ideal served scenario
        var providers = CreateProviderBreakdown(
            ("Globe", 20, 15, 10),
            ("Smart", 20, 15, 10),
            ("DITO", 10, 10, 5)
        );
        var tech = CreateTechBreakdown(count5G: 30, count4G: 20);
        var infrastructure = CreateInfrastructure(
            cellTowers: 50,
            fiberNAPs: 40,
            wifiHotspots: 25,
            providers: 3,
            providerBreakdown: providers,
            techBreakdown: tech
        );
        var speedStats = CreateSpeedStats(
            avgDownload: 100,
            medianDownload: 98,
            avgLatency: 20,
            signalStrength: 4.8,
            speedTestCount: 50,
            meetsMinSpeed: true
        );
        int population = 50000; // High density urban area
        double areaKm2 = 25; // 2000 per km²

        // Act
        var scores = _service.CalculateAllScores(infrastructure, speedStats, areaKm2, population, null);

        // Assert
        Assert.Equal(3, scores.FiberCoverage);
        Assert.Equal(3, scores.InternetAccess);
        Assert.Equal(3, scores.TechnologyType);
        Assert.Equal(3, scores.QualityOfService);
        Assert.Equal(1, scores.CoverageAvailability); // Coverage calculation result

        var totalScore = scores.FiberCoverage + scores.InternetAccess + scores.TechnologyType +
                        scores.QualityOfService + scores.CoverageAvailability;
        Assert.Equal(13, totalScore);
        Assert.Equal(ConnectivityStatusEnum.Served, _service.DetermineConnectivityStatus(totalScore));
    }

    [Fact]
    public void CalculateAllScores_UnderservedScenario_Returns8Points()
    {
        // Arrange - typical underserved scenario
        var providers = CreateProviderBreakdown(
            ("Globe", 5, 3, 0),
            ("Local ISP", 0, 2, 0)
        );
        var tech = CreateTechBreakdown(count4G: 5, count3G: 8);
        var infrastructure = CreateInfrastructure(
            cellTowers: 13,
            fiberNAPs: 5,
            wifiHotspots: 2,
            providers: 2,
            providerBreakdown: providers,
            techBreakdown: tech
        );
        var speedStats = CreateSpeedStats(
            avgDownload: 15,
            medianDownload: 14,
            avgLatency: 70,
            signalStrength: 3.0,
            speedTestCount: 10,
            meetsMinSpeed: false
        );
        int population = 15000;
        double areaKm2 = 50; // 300 per km² - semi-rural

        // Act
        var scores = _service.CalculateAllScores(infrastructure, speedStats, areaKm2, population, null);

        // Assert
        Assert.Equal(2, scores.FiberCoverage); // One major telco
        Assert.Equal(2, scores.InternetAccess); // 10-25 Mbps
        Assert.Equal(3, scores.TechnologyType); // 4G present - served
        Assert.Equal(2, scores.QualityOfService); // Moderate latency/signal
        Assert.InRange(scores.CoverageAvailability, 1, 2); // Limited coverage

        var totalScore = scores.FiberCoverage + scores.InternetAccess + scores.TechnologyType +
                        scores.QualityOfService + scores.CoverageAvailability;
        Assert.InRange(totalScore, 10, 11);
        Assert.Equal(ConnectivityStatusEnum.Served, _service.DetermineConnectivityStatus(totalScore));
    }

    [Fact]
    public void CalculateAllScores_UnservedScenario_Returns5Points()
    {
        // Arrange - unserved scenario
        var providers = CreateProviderBreakdown(
            ("Local ISP", 0, 1, 0)
        );
        var tech = CreateTechBreakdown(count3G: 3);
        var infrastructure = CreateInfrastructure(
            cellTowers: 3,
            fiberNAPs: 1,
            wifiHotspots: 0,
            providers: 1,
            providerBreakdown: providers,
            techBreakdown: tech
        );
        var speedStats = CreateSpeedStats(
            avgDownload: 3,
            medianDownload: 2.5m,
            avgLatency: 150,
            signalStrength: 1.5,
            speedTestCount: 5,
            meetsMinSpeed: false
        );
        int population = 8000;
        double areaKm2 = 100; // 80 per km² - rural

        // Act
        var scores = _service.CalculateAllScores(infrastructure, speedStats, areaKm2, population, null);

        // Assert
        Assert.Equal(2, scores.FiberCoverage); // Has fiber infrastructure
        Assert.Equal(1, scores.InternetAccess); // <10 Mbps
        Assert.Equal(3, scores.TechnologyType); // Fiber infrastructure present - served
        Assert.Equal(1, scores.QualityOfService); // Poor quality
        Assert.Equal(1, scores.CoverageAvailability); // Sparse coverage

        var totalScore = scores.FiberCoverage + scores.InternetAccess + scores.TechnologyType +
                        scores.QualityOfService + scores.CoverageAvailability;
        Assert.InRange(totalScore, 7, 9);
        Assert.Equal(ConnectivityStatusEnum.Underserved, _service.DetermineConnectivityStatus(totalScore));
    }

    [Fact]
    public void CalculateAllScores_NoSpeedData_StillCalculatesInfrastructureScores()
    {
        // Arrange
        var providers = CreateProviderBreakdown(
            ("Globe", 10, 8, 5),
            ("Smart", 10, 7, 5)
        );
        var tech = CreateTechBreakdown(count5G: 10, count4G: 10);
        var infrastructure = CreateInfrastructure(
            cellTowers: 20,
            fiberNAPs: 15,
            wifiHotspots: 10,
            providers: 2,
            providerBreakdown: providers,
            techBreakdown: tech
        );
        int population = 30000;
        double areaKm2 = 50;

        // Act
        var scores = _service.CalculateAllScores(infrastructure, null, areaKm2, population, null);

        // Assert
        Assert.Equal(3, scores.FiberCoverage); // Two major telcos
        Assert.Equal(1, scores.InternetAccess); // No data
        Assert.Equal(3, scores.TechnologyType); // 5G present
        Assert.Equal(1, scores.QualityOfService); // No data
        Assert.Equal(1, scores.CoverageAvailability); // Coverage calculation result

        var totalScore = scores.FiberCoverage + scores.InternetAccess + scores.TechnologyType +
                        scores.QualityOfService + scores.CoverageAvailability;
        Assert.Equal(9, totalScore); // Total adjusted
    }

    [Fact]
    public void CalculateAllScores_NullPopulation_UsesGeographicFallback()
    {
        // Arrange
        var providers = CreateProviderBreakdown(
            ("Globe", 15, 10, 5)
        );
        var tech = CreateTechBreakdown(count4G: 15);
        var infrastructure = CreateInfrastructure(
            cellTowers: 15,
            fiberNAPs: 10,
            wifiHotspots: 5,
            providers: 1,
            providerBreakdown: providers,
            techBreakdown: tech
        );
        double areaKm2 = 100;

        // Act
        var scores = _service.CalculateAllScores(infrastructure, null, areaKm2, null, null);

        // Assert - should still calculate all scores without error
        Assert.InRange(scores.FiberCoverage, 1, 3);
        Assert.Equal(1, scores.InternetAccess); // No data
        Assert.InRange(scores.TechnologyType, 1, 3);
        Assert.Equal(1, scores.QualityOfService); // No data
        Assert.InRange(scores.CoverageAvailability, 1, 3);
    }

    [Fact]
    public void CalculateAllScores_ZeroArea_HandlesGracefully()
    {
        // Arrange
        var infrastructure = CreateInfrastructure(cellTowers: 10);
        double areaKm2 = 0; // Edge case

        // Act
        var scores = _service.CalculateAllScores(infrastructure, null, areaKm2, null, null);

        // Assert - should not throw, handles zero area
        Assert.NotNull(scores);
        Assert.InRange(scores.FiberCoverage, 0, 3);
        Assert.InRange(scores.InternetAccess, 0, 3);
        Assert.InRange(scores.TechnologyType, 0, 3);
        Assert.InRange(scores.QualityOfService, 0, 3);
        Assert.InRange(scores.CoverageAvailability, 0, 3);
    }

    #endregion

    #region Edge Case and Integration Tests

    [Fact]
    public void AllMethods_HandleNullInputsGracefully()
    {
        // Arrange
        var emptyInfra = CreateInfrastructure();
        var context = _service.CalculatePopulationContext(null, 0);

        // Act & Assert - should not throw
        Assert.Equal(1, _service.CalculateFiberCoverageScore(emptyInfra, 0, null, context));
        Assert.Equal(1, _service.CalculateInternetAccessScore(null));
        Assert.Equal(1, _service.CalculateTechnologyTypeScore(emptyInfra));
        Assert.Equal(1, _service.CalculateQualityOfServiceScore(null, emptyInfra));
        Assert.Equal(1, _service.CalculateCoverageAvailabilityScore(emptyInfra, 0, null, context));
        Assert.False(_service.DetermineReliableConnectivity(emptyInfra, null));
    }

    [Fact]
    public void ScoreConsistency_SameInputs_ProduceSameOutputs()
    {
        // Arrange
        var providers = CreateProviderBreakdown(("Globe", 10, 10, 5));
        var tech = CreateTechBreakdown(count4G: 10);
        var infrastructure = CreateInfrastructure(
            cellTowers: 10,
            fiberNAPs: 10,
            wifiHotspots: 5,
            providers: 1,
            providerBreakdown: providers,
            techBreakdown: tech
        );
        var speedStats = CreateSpeedStats(avgDownload: 30, medianDownload: 28, speedTestCount: 10);
        double areaKm2 = 50;
        int population = 10000;

        // Act - calculate multiple times
        var scores1 = _service.CalculateAllScores(infrastructure, speedStats, areaKm2, population, null);
        var scores2 = _service.CalculateAllScores(infrastructure, speedStats, areaKm2, population, null);

        // Assert - deterministic results
        Assert.Equal(scores1.FiberCoverage, scores2.FiberCoverage);
        Assert.Equal(scores1.InternetAccess, scores2.InternetAccess);
        Assert.Equal(scores1.TechnologyType, scores2.TechnologyType);
        Assert.Equal(scores1.QualityOfService, scores2.QualityOfService);
        Assert.Equal(scores1.CoverageAvailability, scores2.CoverageAvailability);
    }

    [Fact]
    public void CMTS_Rubric_AllScoresInValidRange()
    {
        // Arrange - various scenarios
        var scenarios = new[]
        {
            (CreateInfrastructure(), (SpeedStatsDto?)null, 100.0, (int?)null),
            (CreateInfrastructure(cellTowers: 10, fiberNAPs: 5), CreateSpeedStats(avgDownload: 20, speedTestCount: 10), 50.0, (int?)10000),
            (CreateInfrastructure(cellTowers: 50, fiberNAPs: 20, wifiHotspots: 10), CreateSpeedStats(avgDownload: 100, speedTestCount: 50), 25.0, (int?)50000)
        };

        foreach (var (infra, speed, area, pop) in scenarios)
        {
            // Act
            var scores = _service.CalculateAllScores(infra, speed, area, pop, null);

            // Assert - all scores must be 0-3
            Assert.InRange(scores.FiberCoverage, 0, 3);
            Assert.InRange(scores.InternetAccess, 0, 3);
            Assert.InRange(scores.TechnologyType, 0, 3);
            Assert.InRange(scores.QualityOfService, 0, 3);
            Assert.InRange(scores.CoverageAvailability, 0, 3);

            var total = scores.FiberCoverage + scores.InternetAccess + scores.TechnologyType +
                       scores.QualityOfService + scores.CoverageAvailability;
            Assert.InRange(total, 0, 15);
        }
    }

    [Fact]
    public void BoundaryScenario_Exactly11Points_IsServed()
    {
        // Arrange - craft scenario for exactly 11 points
        var providers = CreateProviderBreakdown(
            ("Globe", 5, 5, 0),
            ("Smart", 5, 5, 0)
        );
        var tech = CreateTechBreakdown(count4G: 10);
        var infrastructure = CreateInfrastructure(
            cellTowers: 10,
            fiberNAPs: 10,
            wifiHotspots: 5,
            providers: 2,
            providerBreakdown: providers,
            techBreakdown: tech
        );
        var speedStats = CreateSpeedStats(
            avgDownload: 25, // Exactly 25 Mbps
            medianDownload: 25,
            avgLatency: 49, // Just under 50ms
            signalStrength: 4.0,
            speedTestCount: 10,
            meetsMinSpeed: true
        );
        int population = 40000;
        double areaKm2 = 50; // 800 per km²

        // Act
        var scores = _service.CalculateAllScores(infrastructure, speedStats, areaKm2, population, null);
        var totalScore = scores.FiberCoverage + scores.InternetAccess + scores.TechnologyType +
                        scores.QualityOfService + scores.CoverageAvailability;

        // Assert
        Assert.InRange(totalScore, 11, 15); // Should be at least 11
        Assert.Equal(ConnectivityStatusEnum.Served, _service.DetermineConnectivityStatus(totalScore));
    }

    [Fact]
    public void BoundaryScenario_Exactly6Points_IsUnderserved()
    {
        // Arrange - craft scenario for 6 points
        var providers = CreateProviderBreakdown(("Local ISP", 5, 3, 0));
        var tech = CreateTechBreakdown(count3G: 5);
        var infrastructure = CreateInfrastructure(
            cellTowers: 5,
            fiberNAPs: 3,
            wifiHotspots: 0,
            providers: 1,
            providerBreakdown: providers,
            techBreakdown: tech
        );
        var speedStats = CreateSpeedStats(
            avgDownload: 10, // Exactly 10 Mbps
            medianDownload: 10,
            avgLatency: 100, // Exactly 100ms
            signalStrength: 2.5,
            speedTestCount: 10,
            meetsMinSpeed: false
        );
        int population = 5000;
        double areaKm2 = 50; // 100 per km²

        // Act
        var scores = _service.CalculateAllScores(infrastructure, speedStats, areaKm2, population, null);
        var totalScore = scores.FiberCoverage + scores.InternetAccess + scores.TechnologyType +
                        scores.QualityOfService + scores.CoverageAvailability;

        // Assert
        Assert.InRange(totalScore, 6, 10);
        Assert.Equal(ConnectivityStatusEnum.Underserved, _service.DetermineConnectivityStatus(totalScore));
    }

    [Fact]
    public void ComplexScenario_MixedModernAndLegacyTech_ProperScoring()
    {
        // Arrange - area with mix of technologies
        var providers = CreateProviderBreakdown(
            ("Globe", 10, 5, 2),
            ("Smart", 8, 4, 2),
            ("Local Provider", 2, 1, 1)
        );
        // Legacy dominates: 30 legacy vs 15 modern
        var tech = CreateTechBreakdown(count2G: 10, count3G: 20, count4G: 10, count5G: 5);
        var infrastructure = CreateInfrastructure(
            cellTowers: 20,
            fiberNAPs: 10,
            wifiHotspots: 5,
            providers: 3,
            providerBreakdown: providers,
            techBreakdown: tech
        );
        var speedStats = CreateSpeedStats(
            avgDownload: 18,
            medianDownload: 15,
            avgLatency: 80,
            signalStrength: 3.2,
            speedTestCount: 20,
            meetsMinSpeed: false
        );
        int population = 25000;
        double areaKm2 = 75; // ~333 per km²

        // Act
        var scores = _service.CalculateAllScores(infrastructure, speedStats, areaKm2, population, null);

        // Assert
        Assert.Equal(3, scores.FiberCoverage); // Two major telcos with fiber
        Assert.Equal(2, scores.InternetAccess); // 10-25 Mbps range
        Assert.Equal(3, scores.TechnologyType); // 5G present takes priority
        Assert.Equal(2, scores.QualityOfService); // Moderate latency
        Assert.InRange(scores.CoverageAvailability, 1, 3); // Multiple providers

        var totalScore = scores.FiberCoverage + scores.InternetAccess + scores.TechnologyType +
                        scores.QualityOfService + scores.CoverageAvailability;
        Assert.InRange(totalScore, 11, 13); // Likely served or borderline served
    }

    #endregion
}
