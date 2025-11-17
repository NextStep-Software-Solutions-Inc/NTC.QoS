using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Infrastructure.Seed.Interfaces;
using NTC.QoS.Infrastructure.Seed.Models;

namespace NTC.QoS.Infrastructure.Seed.Parsers;

/// <summary>
/// Streaming parser for GeoJSON location data.
/// Parses features one at a time to minimize memory usage.
/// </summary>
public class StreamingGeoJsonParser : ILocationDataParser<ParsedLocationFeature>
{
    private readonly ILogger<StreamingGeoJsonParser> _logger;
    private readonly string _adminLevel;
    private readonly string? _parentAdminLevel;

    public StreamingGeoJsonParser(
        ILogger<StreamingGeoJsonParser> logger,
        string adminLevel,
        string? parentAdminLevel = null)
    {
        _logger = logger;
        _adminLevel = adminLevel;
        _parentAdminLevel = parentAdminLevel;
    }

    public async IAsyncEnumerable<ParsedLocationFeature> ParseFeaturesAsync(
        Stream stream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var streamReader = new StreamReader(stream, leaveOpen: false);
        var serializer = GeoJsonSerializer.Create();
        using var jsonReader = new Newtonsoft.Json.JsonTextReader(streamReader);

        FeatureCollection? featureCollection = null;
        
        try
        {
            // Async yield to allow compiler to understand this is truly async
            await Task.Yield();
            featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize GeoJSON stream");
            yield break;
        }

        if (featureCollection == null || featureCollection.Count == 0)
        {
            _logger.LogWarning("No features found in GeoJSON stream");
            yield break;
        }

        var processedCount = 0;
        var skippedCount = 0;

        foreach (var feature in featureCollection)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var parsed = TryParseFeature(feature);
            if (parsed.HasValue)
            {
                yield return parsed.Value;
                processedCount++;
                
                if (processedCount % 1000 == 0)
                {
                    _logger.LogDebug("Parsed {Count} features from GeoJSON stream", processedCount);
                }
            }
            else
            {
                skippedCount++;
            }
        }

        _logger.LogInformation(
            "Parsing completed. Processed: {ProcessedCount}, Skipped: {SkippedCount}",
            processedCount, skippedCount);
    }

    private ParsedLocationFeature? TryParseFeature(IFeature feature)
    {
        try
        {
            var attributes = feature.Attributes;
            if (attributes == null)
            {
                _logger.LogWarning("Feature skipped: no attributes");
                return null;
            }

            // Required fields
            var codeKey = $"{_adminLevel}_PCODE";
            var nameKey = $"{_adminLevel}_EN";
            
            if (!attributes.Exists(codeKey))
            {
                _logger.LogWarning("Feature skipped: missing {CodeKey}", codeKey);
                return null;
            }
            if (!attributes.Exists(nameKey))
            {
                _logger.LogWarning("Feature skipped: missing {NameKey}", nameKey);
                return null;
            }
            if (!attributes.Exists("AREA_SQKM"))
            {
                _logger.LogWarning("Feature skipped: missing AREA_SQKM");
                return null;
            }

            var code = attributes[codeKey]?.ToString();
            var name = attributes[nameKey]?.ToString();
            
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("Feature skipped: empty or whitespace code");
                return null;
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Feature skipped: empty or whitespace name");
                return null;
            }

            var area = attributes["AREA_SQKM"] != null && 
                       float.TryParse(attributes["AREA_SQKM"].ToString(), out var areaValue) 
                ? areaValue : 0f;

            var boundaries = ExtractBoundaries(feature);
            if (boundaries == null)
            {
                _logger.LogWarning("Feature skipped: invalid geometry (not MultiPolygon or Polygon)");
                return null;
            }

            // Optional parent code
            string? parentCode = null;
            if (_parentAdminLevel != null)
            {
                var parentKey = $"{_parentAdminLevel}_PCODE";
                parentCode = attributes.Exists(parentKey) ? attributes[parentKey]?.ToString() : null;
            }

            // Optional population data
            var totalPop = TryParsePopulation(attributes, "TOTAL_POP");
            var householdPop = TryParsePopulation(attributes, "HHOLD_POPN");
            var householdCount = TryParsePopulation(attributes, "HHOLD_CNT");

            return new ParsedLocationFeature
            {
                Code = code,
                Name = name,
                Area = area,
                Boundaries = boundaries,
                ParentCode = parentCode,
                TotalPopulation = totalPop,
                HouseholdPopulation = householdPop,
                HouseholdCount = householdCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse feature, skipping");
            return null;
        }
    }

    private static MultiPolygon? ExtractBoundaries(IFeature feature)
    {
        return feature.Geometry switch
        {
            MultiPolygon multiPolygon => multiPolygon,
            Polygon polygon => GeometryHelper.CreateMultiPolygon(null, polygon),
            _ => null
        };
    }

    private int? TryParsePopulation(IAttributesTable attributes, string key)
    {
        if (!attributes.Exists(key))
            return null;

        var value = attributes[key]?.ToString();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleaned = value.Replace(",", "");
        return int.TryParse(cleaned, out var result) ? result : null;
    }
}
