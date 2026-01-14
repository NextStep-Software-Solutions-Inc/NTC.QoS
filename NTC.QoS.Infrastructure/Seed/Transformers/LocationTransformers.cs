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
    private static int _missingParentCodeCount = 0;
    private static int _parentNotFoundCount = 0;
    private static bool _logged = false;

    public Barangay? Transform(ParsedLocationFeature feature, TransformContext context)
    {
        if (string.IsNullOrEmpty(feature.ParentCode))
        {
            _missingParentCodeCount++;
            if (!_logged && _missingParentCodeCount == 1)
            {
                Console.WriteLine($"[BarangayTransformer] First missing ParentCode - Code: {feature.Code}, Name: {feature.Name}");
            }
            return null;
        }

        if (!context.ParentIdMap.TryGetValue(feature.ParentCode, out var municipalityId))
        {
            _parentNotFoundCount++;
            if (!_logged && _parentNotFoundCount == 1)
            {
                Console.WriteLine($"[BarangayTransformer] First parent not found - ParentCode: {feature.ParentCode}, Barangay: {feature.Name}, Available municipality count: {context.ParentIdMap.Count}");
                if (context.ParentIdMap.Count > 0)
                {
                    Console.WriteLine($"[BarangayTransformer] Sample municipality codes: {string.Join(", ", context.ParentIdMap.Keys.Take(5))}");
                }
            }
            if (_parentNotFoundCount + _missingParentCodeCount >= 42000 && !_logged)
            {
                Console.WriteLine($"[BarangayTransformer] Summary - Missing ParentCode: {_missingParentCodeCount}, Parent Not Found: {_parentNotFoundCount}");
                _logged = true;
            }
            return null;
        }

        return new Barangay
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
            MunicipalityId = municipalityId,
            IsDeleted = false,
            CreatedAt = context.Timestamp,
            CreatedBy = context.UserId,
            UpdatedAt = context.Timestamp,
            UpdatedBy = context.UserId
        };
    }
}
