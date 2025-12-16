using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Seed.Interfaces;
using NTC.QoS.Infrastructure.Seed.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.Simplify;

namespace NTC.QoS.Infrastructure.Seed.Transformers;

/// <summary>
/// Transforms parsed GeoJSON features into Region entities.
/// </summary>
public class RegionTransformer : ILocationDataTransformer<ParsedLocationFeature, Region>
{
    public Region? Transform(ParsedLocationFeature feature, TransformContext context)
    {
        return new Region
        {
            Id = Guid.NewGuid(),
            Code = feature.Code,
            Name = feature.Name,
            Description = null,
            Area = feature.Area,
            TotalPopulation = feature.TotalPopulation,
            HouseholdPopulation = feature.HouseholdPopulation,
            HouseholdCount = feature.HouseholdCount,
            Boundaries = feature.Boundaries,
            Center = feature.Boundaries.Centroid,
            IsDeleted = false,
            CreatedAt = context.Timestamp,
            CreatedBy = context.UserId,
            UpdatedAt = context.Timestamp,
            UpdatedBy = context.UserId
        };
    }
}

/// <summary>
/// Transforms parsed GeoJSON features into Province entities.
/// </summary>
public class ProvinceTransformer : ILocationDataTransformer<ParsedLocationFeature, Province>
{
    public Province? Transform(ParsedLocationFeature feature, TransformContext context)
    {
        if (string.IsNullOrEmpty(feature.ParentCode))
            return null;

        if (!context.ParentIdMap.TryGetValue(feature.ParentCode, out var regionId))
            return null;

        return new Province
        {
            Id = Guid.NewGuid(),
            Code = feature.Code,
            Name = feature.Name,
            Description = null,
            Area = feature.Area,
            TotalPopulation = feature.TotalPopulation,
            HouseholdPopulation = feature.HouseholdPopulation,
            HouseholdCount = feature.HouseholdCount,
            Boundaries = feature.Boundaries,
            Center = feature.Boundaries.Centroid,
            RegionId = regionId,
            IsDeleted = false,
            CreatedAt = context.Timestamp,
            CreatedBy = context.UserId,
            UpdatedAt = context.Timestamp,
            UpdatedBy = context.UserId
        };
    }
}

/// <summary>
/// Transforms parsed GeoJSON features into Municipality entities.
/// </summary>
public class MunicipalityTransformer : ILocationDataTransformer<ParsedLocationFeature, Municipality>
{
    public Municipality? Transform(ParsedLocationFeature feature, TransformContext context)
    {
        if (string.IsNullOrEmpty(feature.ParentCode))
            return null;

        if (!context.ParentIdMap.TryGetValue(feature.ParentCode, out var provinceId))
            return null;

        var isCity = feature.Name.Contains("City", StringComparison.OrdinalIgnoreCase) ||
                     feature.Name.Contains("Capital", StringComparison.OrdinalIgnoreCase);

        return new Municipality
        {
            Id = Guid.NewGuid(),
            Code = feature.Code,
            Name = feature.Name,
            Description = null,
            Area = feature.Area,
            TotalPopulation = feature.TotalPopulation,
            HouseholdPopulation = feature.HouseholdPopulation,
            HouseholdCount = feature.HouseholdCount,
            Boundaries = feature.Boundaries,
            Center = feature.Boundaries.Centroid,
            ProvinceId = provinceId,
            IsCity = isCity,
            IsDeleted = false,
            CreatedAt = context.Timestamp,
            CreatedBy = context.UserId,
            UpdatedAt = context.Timestamp,
            UpdatedBy = context.UserId
        };
    }
}

/// <summary>
/// Transforms parsed GeoJSON features into Barangay entities.
/// </summary>
public class BarangayTransformer : ILocationDataTransformer<ParsedLocationFeature, Barangay>
{
    public Barangay? Transform(ParsedLocationFeature feature, TransformContext context)
    {
        if (string.IsNullOrEmpty(feature.ParentCode))
            return null;

        if (!context.ParentIdMap.TryGetValue(feature.ParentCode, out var municipalityId))
            return null;

        return new Barangay
        {
            Id = Guid.NewGuid(),
            Code = feature.Code,
            Name = feature.Name,
            Description = null,
            Area = feature.Area,
            TotalPopulation = null, // Barangays don't have population data in the GeoJSON
            HouseholdPopulation = null,
            HouseholdCount = null,
            Boundaries = feature.Boundaries,
            Center = feature.Boundaries.Centroid,
            MunicipalityId = municipalityId,
            IsDeleted = false,
            CreatedAt = context.Timestamp,
            CreatedBy = context.UserId,
            UpdatedAt = context.Timestamp,
            UpdatedBy = context.UserId
        };
    }
}
