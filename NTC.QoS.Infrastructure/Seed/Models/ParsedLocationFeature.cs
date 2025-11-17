using NetTopologySuite.Geometries;

namespace NTC.QoS.Infrastructure.Seed.Models;

/// <summary>
/// Lightweight struct for parsed GeoJSON feature data.
/// Reduces memory footprint by using value types where possible.
/// </summary>
public readonly struct ParsedLocationFeature
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required float Area { get; init; }
    public required MultiPolygon Boundaries { get; init; }
    public string? ParentCode { get; init; }
    public int? TotalPopulation { get; init; }
    public int? HouseholdPopulation { get; init; }
    public int? HouseholdCount { get; init; }
}
