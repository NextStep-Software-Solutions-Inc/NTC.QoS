using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Infrastructure.Seed.Interfaces;
using NTC.QoS.Infrastructure.Seed.Models;
using System.Diagnostics;

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
        _logger.LogInformation("Starting to parse GeoJSON features from stream");
        using var streamReader = new StreamReader(stream, leaveOpen: false);
        using var jsonReader = new Newtonsoft.Json.JsonTextReader(streamReader);
        var serializer = GeoJsonSerializer.Create();

        // Skip to the start of the features array
        while (await jsonReader.ReadAsync(cancellationToken) && jsonReader.TokenType != Newtonsoft.Json.JsonToken.StartArray)
        {
            if (jsonReader.TokenType == Newtonsoft.Json.JsonToken.PropertyName &&
                jsonReader.Value?.ToString() == "features")
            {
                await jsonReader.ReadAsync(cancellationToken); // Move to StartArray
                break;
            }
        }

        if (jsonReader.TokenType != Newtonsoft.Json.JsonToken.StartArray)
        {
            _logger.LogError("Invalid GeoJSON: 'features' array not found");
            yield break;
        }

        var processedCount = 0;
        var skippedCount = 0;

        while (await jsonReader.ReadAsync() && jsonReader.TokenType != Newtonsoft.Json.JsonToken.EndArray)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (jsonReader.TokenType == Newtonsoft.Json.JsonToken.StartObject)
            {
                _logger.LogDebug("Starting to process GeoJSON feature at position {Position}", jsonReader.LineNumber);
                IFeature? feature = null;
                try
                {
                    // Deserialize individual feature on a background thread to avoid blocking
                    var stopwatch = Stopwatch.StartNew();
                    feature = await Task.Run(() => serializer.Deserialize<IFeature>(jsonReader));
                    stopwatch.Stop();
                    _logger.LogDebug("Successfully deserialized GeoJSON feature in {Time}ms", stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize individual feature, skipping");
                    skippedCount++;
                    // Skip to the end of the current object to continue parsing
                    await jsonReader.SkipAsync();
                    continue;
                }

                if (feature != null)
                {
                    var parsed = TryParseFeature(feature);
                    if (parsed.HasValue)
                    {
                        _logger.LogDebug("Successfully parsed feature with code {Code}", parsed.Value.Code);
                        yield return parsed.Value;
                        processedCount++;

                        if (processedCount % 1000 == 0)
                        {
                            _logger.LogDebug("Parsed {Count} features from GeoJSON stream", processedCount);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Feature parsing failed, skipping");
                        skippedCount++;
                    }
                }
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
