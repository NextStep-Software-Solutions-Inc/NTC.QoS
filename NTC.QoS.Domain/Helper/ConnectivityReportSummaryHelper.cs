using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Domain.Helper;

/// <summary>
/// Shared helper for building municipality, province, and region connectivity summaries
/// from a flat list of <see cref="ConnectivityAreaDetailDto"/> items.
/// Used by both synchronous download handlers and async background report jobs.
/// </summary>
public static class ConnectivityReportSummaryHelper
{
    /// <summary>
    /// Groups areas by (MunicipalityName, ProvinceName) and produces one
    /// <see cref="GeographicLevelSummaryDto"/> per unique municipality.
    /// </summary>
    public static List<GeographicLevelSummaryDto> BuildMunicipalitySummaries(
        List<ConnectivityAreaDetailDto> areas,
        IReadOnlyDictionary<(string Name, string Province), string>? assessedStatuses = null)
    {
        return areas
            .GroupBy(a => new
            {
                MunicipalityName = string.IsNullOrWhiteSpace(a.MunicipalityName) ? "Unknown" : a.MunicipalityName,
                ProvinceName = string.IsNullOrWhiteSpace(a.ProvinceName) ? "Unknown" : a.ProvinceName
            })
            .Select(g =>
            {
                string? assessedStatus = null;
                assessedStatuses?.TryGetValue((g.Key.MunicipalityName, g.Key.ProvinceName), out assessedStatus);
                return AggregateToSummary(
                    name: g.Key.MunicipalityName,
                    parentName: g.Key.ProvinceName,
                    level: "Municipality",
                    areas: g.ToList(),
                    assessedStatus: assessedStatus);
            })
            .OrderBy(s => s.ParentName)
            .ThenBy(s => s.Name)
            .ToList();
    }

    /// <summary>
    /// Groups areas by ProvinceName and produces one
    /// <see cref="GeographicLevelSummaryDto"/> per unique province.
    /// </summary>
    public static List<GeographicLevelSummaryDto> BuildProvinceSummaries(
        List<ConnectivityAreaDetailDto> areas,
        IReadOnlyDictionary<string, string>? assessedStatuses = null)
    {
        return areas
            .GroupBy(a => string.IsNullOrWhiteSpace(a.ProvinceName) ? "Unknown" : a.ProvinceName)
            .Select(g =>
            {
                string? assessedStatus = null;
                assessedStatuses?.TryGetValue(g.Key, out assessedStatus);
                return AggregateToSummary(
                    name: g.Key,
                    parentName: null,
                    level: "Province",
                    areas: g.ToList(),
                    assessedStatus: assessedStatus);
            })
            .OrderBy(s => s.Name)
            .ToList();
    }

    /// <summary>
    /// Produces a single regional summary over all <paramref name="areas"/>,
    /// or <c>null</c> when the list is empty.
    /// The <see cref="GeographicLevelSummaryDto.Level"/> is set to <c>"Regional Summary"</c> regardless
    /// of the geographic scope of the report, so it remains accurate for national,
    /// region-, province-, or municipality-scoped reports alike.
    /// </summary>
    public static GeographicLevelSummaryDto? BuildRegionSummary(
        List<ConnectivityAreaDetailDto> areas,
        string? assessedStatus = null)
    {
        if (areas.Count == 0) return null;

        return AggregateToSummary(
            name: "Regional Summary",
            parentName: null,
            level: "Regional Summary",
            areas: areas,
            assessedStatus: assessedStatus);
    }

    /// <summary>
    /// Computes an aggregated <see cref="GeographicLevelSummaryDto"/> from the given areas.
    /// </summary>
    public static GeographicLevelSummaryDto AggregateToSummary(
        string name,
        string? parentName,
        string level,
        List<ConnectivityAreaDetailDto> areas,
        string? assessedStatus = null)
    {
        if (areas.Count == 0)
        {
            return new GeographicLevelSummaryDto
            {
                Name = name,
                ParentName = parentName,
                Level = level,
                OverallConnectivityStatus = "Unserved",
                AssessedConnectivityStatus = assessedStatus
            };
        }

        var avgTotal = areas.Average(a => a.TotalScore);

        return new GeographicLevelSummaryDto
        {
            Name = name,
            ParentName = parentName,
            Level = level,
            TotalBarangays = areas.Count,
            ServedBarangays = areas.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Served),
            UnderservedBarangays = areas.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved),
            UnservedBarangays = areas.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved),
            TotalPopulation = areas.Sum(a => (long)(a.Population ?? 0)),
            ServedPopulation = areas
                .Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Served)
                .Sum(a => (long)(a.Population ?? 0)),
            UnderservedPopulation = areas
                .Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved)
                .Sum(a => (long)(a.Population ?? 0)),
            UnservedPopulation = areas
                .Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved)
                .Sum(a => (long)(a.Population ?? 0)),
            AverageFiberCoverageScore = areas.Average(a => a.FiberCoverageScore),
            AverageInternetAccessScore = areas.Average(a => a.InternetAccessScore),
            AverageTechnologyTypeScore = areas.Average(a => a.TechnologyTypeScore),
            AverageQualityOfServiceScore = areas.Average(a => a.QualityOfServiceScore),
            AverageCoverageAvailabilityScore = areas.Average(a => a.CoverageAvailabilityScore),
            AverageTotalScore = avgTotal,
            OverallConnectivityStatus = avgTotal >= 11 ? "Served"
                : avgTotal >= 6 ? "Underserved"
                : "Unserved",
            AssessedConnectivityStatus = assessedStatus
        };
    }
}
