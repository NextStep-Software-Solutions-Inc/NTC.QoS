using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Infrastructure.Seed.Interfaces;
using NTC.QoS.Infrastructure.Seed.Models;
using System.Diagnostics;
using System.IO;

namespace NTC.QoS.Infrastructure.Seed.Parsers;

/// <summary>
/// Streaming parser for GeoJSON location data.
/// Parses features one at a time to minimize memory usage.
/// </summary>
public class StreamingGeoJsonParser(
    ILogger<StreamingGeoJsonParser> logger,
    string adminLevel,
    string? parentAdminLevel = null) : ILocationDataParser<ParsedLocationFeature>
{

    public async IAsyncEnumerable<ParsedLocationFeature> ParseFeaturesAsync(
        Stream stream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? tempFile = null;
        try
        {
            logger.LogInformation("Starting to parse GeoJSON features from stream");

            // Buffer the stream to a temp file to minimize memory usage
            tempFile = Path.GetTempFileName();
            using (var fileStream = File.OpenWrite(tempFile))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            using var fileReadStream = File.OpenRead(tempFile);
            using var streamReader = new StreamReader(fileReadStream, leaveOpen: false);
            using var jsonReader = new Newtonsoft.Json.JsonTextReader(streamReader);
            var serializer = GeoJsonSerializer.Create();

            // Skip to the start of the features array
            while (await jsonReader.ReadAsync(cancellationToken) && jsonReader.TokenType != Newtonsoft.Json.JsonToken.StartArray)
            {
                logger.LogDebug("Read token: {TokenType} at line {Line}", jsonReader.TokenType, jsonReader.LineNumber);
                if (jsonReader.TokenType == Newtonsoft.Json.JsonToken.PropertyName &&
                    jsonReader.Value?.ToString() == "features")
                {
                    await jsonReader.ReadAsync(cancellationToken); // Move to StartArray
                    break;
                }
            }

            if (jsonReader.TokenType != Newtonsoft.Json.JsonToken.StartArray)
            {
                logger.LogError("Invalid GeoJSON: 'features' array not found");
                yield break;
            }

            var processedCount = 0;
            var skippedCount = 0;

            while (await jsonReader.ReadAsync(cancellationToken) && jsonReader.TokenType != Newtonsoft.Json.JsonToken.EndArray)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                if (jsonReader.TokenType == Newtonsoft.Json.JsonToken.StartObject)
                {
                    logger.LogDebug("Starting to process GeoJSON feature at position {Position}", jsonReader.LineNumber);
                    IFeature? feature = null;
                    try
                    {
                        // Deserialize individual feature on a background thread to avoid blocking
                        var stopwatch = Stopwatch.StartNew();
                        feature = await Task.Run(() => serializer.Deserialize<IFeature>(jsonReader));
                        stopwatch.Stop();
                        logger.LogDebug("Successfully deserialized GeoJSON feature in {Time}ms", stopwatch.ElapsedMilliseconds);
                    }
                    catch
                    {
                        logger.LogWarning("Failed to deserialize individual feature, skipping due to large geometry");
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
                            logger.LogDebug("Successfully parsed feature with code {Code}", parsed.Value.Code);
                            yield return parsed.Value;
                            processedCount++;

                            if (processedCount % 1000 == 0)
                            {
                                logger.LogDebug("Parsed {Count} features from GeoJSON stream", processedCount);
                            }
                        }
                        else
                        {
                            logger.LogDebug("Feature parsing failed, skipping");
                            skippedCount++;
                        }
                    }
                }
            }

            logger.LogInformation(
                "Parsing completed. Processed: {ProcessedCount}, Skipped: {SkippedCount}",
                processedCount, skippedCount);
        }
        finally
        {
            if (tempFile != null && File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
    private ParsedLocationFeature? TryParseFeature(IFeature feature)
    {
        try
        {
            var attributes = feature.Attributes;
            if (attributes == null)
            {
                logger.LogWarning("Feature skipped: no attributes");
                return null;
            }

            // Required fields
            var codeKey = $"{adminLevel}_PCODE";
            var nameKey = $"{adminLevel}_EN";

            if (!attributes.Exists(codeKey))
            {
                logger.LogWarning("Feature skipped: missing {CodeKey}", codeKey);
                return null;
            }
            if (!attributes.Exists(nameKey))
            {
                logger.LogWarning("Feature skipped: missing {NameKey}", nameKey);
                return null;
            }
            if (!attributes.Exists("AREA_SQKM"))
            {
                logger.LogWarning("Feature skipped: missing AREA_SQKM");
                return null;
            }

            var code = attributes[codeKey]?.ToString();
            var name = attributes[nameKey]?.ToString();

            if (string.IsNullOrWhiteSpace(code))
            {
                logger.LogWarning("Feature skipped: empty or whitespace code");
                return null;
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                logger.LogWarning("Feature skipped: empty or whitespace name");
                return null;
            }

            var area = attributes["AREA_SQKM"] != null &&
                       float.TryParse(attributes["AREA_SQKM"].ToString(), out var areaValue)
                ? areaValue : 0f;

            var boundaries = ExtractBoundaries(feature);
            if (boundaries == null)
            {
                logger.LogWarning("Feature skipped: invalid geometry (not MultiPolygon or Polygon)");
                return null;
            }

            // Optional parent code
            string? parentCode = null;
            if (parentAdminLevel != null)
            {
                var parentKey = $"{parentAdminLevel}_PCODE";
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
            logger.LogWarning(ex, "Failed to parse feature, skipping");
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

    private static int? TryParsePopulation(IAttributesTable attributes, string key)
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
