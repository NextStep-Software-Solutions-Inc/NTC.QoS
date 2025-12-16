using System.Text.Json;
using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// Custom JSON converter for DetailedTechnologyBandBreakdownDto to serialize both dictionary entries and properties
/// </summary>
public class DetailedTechnologyBandBreakdownDtoJsonConverter : JsonConverter<DetailedTechnologyBandBreakdownDto>
{
    public override DetailedTechnologyBandBreakdownDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new DetailedTechnologyBandBreakdownDto();
        
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return result;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            string propertyName = reader.GetString()!;
            reader.Read();

            // Handle the additional properties
            switch (propertyName)
            {
                case "availableTechnologies":
                    result.AvailableTechnologies = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
                    break;
                case "availableGenerations":
                    result.AvailableGenerations = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
                    break;
                case "allBands":
                    result.AllBands = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
                    break;
                case "count2G":
                    result.Count2G = reader.GetInt32();
                    break;
                case "count3G":
                    result.Count3G = reader.GetInt32();
                    break;
                case "count4G":
                    result.Count4G = reader.GetInt32();
                    break;
                case "count5G":
                    result.Count5G = reader.GetInt32();
                    break;
                // Boolean availability properties (computed from counts, skip during deserialization)
                case "has2G":
                case "has3G":
                case "has4G":
                case "has5G":
                    reader.Skip();
                    break;
                case "dominantGeneration":
                    result.DominantGeneration = reader.GetString();
                    break;
                case "primaryTechnology":
                    result.PrimaryTechnology = reader.GetString() ?? string.Empty;
                    break;
                case "hasModernCoverage":
                    result.HasModernCoverage = reader.GetBoolean();
                    break;
                case "modernCoverageRatio":
                    result.ModernCoverageRatio = reader.GetDouble();
                    break;
                case "totalTechnologyCount":
                    result.TotalTechnologyCount = reader.GetInt32();
                    break;
                // Computed properties - skip during deserialization
                case "hasModernTechnology":
                case "isLegacyDominated":
                case "totalUniqueBands":
                case "hasBands":
                    reader.Skip();
                    break;
                // Dictionary entries (technology -> TechnologyBandDetailDto)
                default:
                    var bandDetail = JsonSerializer.Deserialize<TechnologyBandDetailDto>(ref reader, options);
                    if (bandDetail != null)
                    {
                        result[propertyName] = bandDetail;
                    }
                    break;
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, DetailedTechnologyBandBreakdownDto value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Write dictionary entries first (technology -> band detail mapping)
        foreach (var kvp in value)
        {
            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(kvp.Key) ?? kvp.Key);
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }

        // Write additional summary properties
        writer.WritePropertyName("availableTechnologies");
        JsonSerializer.Serialize(writer, value.AvailableTechnologies, options);

        writer.WritePropertyName("availableGenerations");
        JsonSerializer.Serialize(writer, value.AvailableGenerations, options);

        writer.WritePropertyName("allBands");
        JsonSerializer.Serialize(writer, value.AllBands, options);

        writer.WriteNumber("count2G", value.Count2G);
        writer.WriteNumber("count3G", value.Count3G);
        writer.WriteNumber("count4G", value.Count4G);
        writer.WriteNumber("count5G", value.Count5G);

        // Write boolean availability properties
        writer.WriteBoolean("has2G", value.Has2G);
        writer.WriteBoolean("has3G", value.Has3G);
        writer.WriteBoolean("has4G", value.Has4G);
        writer.WriteBoolean("has5G", value.Has5G);

        if (!string.IsNullOrEmpty(value.DominantGeneration))
        {
            writer.WriteString("dominantGeneration", value.DominantGeneration);
        }
        else
        {
            writer.WriteNull("dominantGeneration");
        }

        writer.WriteString("primaryTechnology", value.PrimaryTechnology);

        writer.WriteBoolean("hasModernTechnology", value.HasModernTechnology);
        writer.WriteBoolean("isLegacyDominated", value.IsLegacyDominated);
        writer.WriteBoolean("hasModernCoverage", value.HasModernCoverage);
        writer.WriteNumber("modernCoverageRatio", value.ModernCoverageRatio);

        writer.WriteNumber("totalTechnologyCount", value.TotalTechnologyCount);
        writer.WriteNumber("totalUniqueBands", value.TotalUniqueBands);
        writer.WriteBoolean("hasBands", value.HasBands);

        writer.WriteEndObject();
    }
}
