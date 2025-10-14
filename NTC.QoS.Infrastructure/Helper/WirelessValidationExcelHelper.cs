using NTC.QoS.Domain.Models.ExcelBindings;
using System.Reflection;

namespace NTC.QoS.Infrastructure.Helper;

public class WirelessValidationExcelHelper
{
    public static List<WirelessValidationRecord> ParseExcelToWirelessValidationRecords(FileStream stream)
    {
        var result = new List<WirelessValidationRecord>();
        using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
        {
            var worksheet = workbook.Worksheet(1);
            if (worksheet == null)
                return result;

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            if (lastRow < 4 || lastColumn == 0)
                return result;

            // Parse municipality information from cell A1
            // Expected format: "<municipalityName>, <provinceName>, <regionName>"
            var (Municipality, Province, Region) = ParseMunicipalityFromA1(worksheet.Cell(1, 1).GetString());

            // Read headers from row 2 (Generation), row 3 (Technology), and row 4 (Band headers)
            var generationHeaders = new List<string>();
            var technologyHeaders = new List<string>();
            var bandHeaders = new List<string>();

            for (int col = 1; col <= lastColumn; col++)
            {
                generationHeaders.Add(worksheet.Cell(2, col).GetString().Trim().ToUpperInvariant());
                technologyHeaders.Add(worksheet.Cell(3, col).GetString().Trim().ToUpperInvariant());
                bandHeaders.Add(worksheet.Cell(4, col).GetString().Trim().ToUpperInvariant());
            }

            // Create property mapping based on generation + technology + band headers
            var properties = typeof(WirelessValidationRecord).GetProperties();
            var propertyMap = CreatePropertyMappingWithGenerationAndTechnology(generationHeaders, technologyHeaders, bandHeaders, properties);

            // Process data rows starting from row 5 (after headers in row 4)
            for (int row = 5; row <= lastRow; row++)
            {
                var record = new WirelessValidationRecord
                {
                    // Set municipality info parsed from A1
                    Municipality = Municipality,
                    Province = Province,
                    Region = Region
                };

                for (int col = 1; col <= lastColumn; col++)
                {
                    var cellValue = worksheet.Cell(row, col).GetString();

                    if (propertyMap.TryGetValue(col, out var property) && property.CanWrite)
                    {
                        try
                        {
                            SetPropertyValue(record, property, cellValue);
                        }
                        catch (Exception ex)
                        {
                            var generation = col <= generationHeaders.Count ? generationHeaders[col - 1] : "Unknown";
                            var technology = col <= technologyHeaders.Count ? technologyHeaders[col - 1] : "Unknown";
                            var band = col <= bandHeaders.Count ? bandHeaders[col - 1] : "Unknown";
                            throw new InvalidOperationException($"Error converting value '{cellValue}' to type '{property.PropertyType.Name}' for property '{property.Name}' at row {row}, column {col} (Generation: '{generation}', Technology: '{technology}', Band: '{band}').", ex);
                        }
                    }
                }

                // Construct the complete address from all available components (barangay, municipality, province, region)
                // This ensures consistent address format regardless of missing barangay
                record.Address = string.Join(", ", new[] { record.Barangay, record.Municipality, record.Province, record.Region }
                    .Where(component => !string.IsNullOrWhiteSpace(component)));

                result.Add(record);
            }
        }
        return result;
    }

    private static (string Municipality, string Province, string Region) ParseMunicipalityFromA1(string cellA1Value)
    {
        if (string.IsNullOrWhiteSpace(cellA1Value))
        {
            return ("Unknown", "Unknown", "Unknown");
        }

        // Expected format: "<municipalityName>, <provinceName>, <regionName>"
        var parts = cellA1Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToArray();

        return parts.Length switch
        {
            >= 3 => (parts[0], parts[1], parts[2]),
            2 => (parts[0], parts[1], "Unknown"),
            1 => (parts[0], "Unknown", "Unknown"),
            _ => ("Unknown", "Unknown", "Unknown")
        };
    }

    private static Dictionary<int, PropertyInfo> CreatePropertyMappingWithGenerationAndTechnology(
        List<string> generationHeaders,
        List<string> technologyHeaders,
        List<string> bandHeaders,
        PropertyInfo[] properties)
    {
        var propertyMap = new Dictionary<int, PropertyInfo>();

        // Track which properties have been used to avoid duplicate mappings
        var usedProperties = new HashSet<string>();

        for (int col = 0; col < bandHeaders.Count; col++)
        {
            var generation = col < generationHeaders.Count ? generationHeaders[col] : string.Empty;
            var technology = col < technologyHeaders.Count ? technologyHeaders[col] : string.Empty;
            var bandHeader = bandHeaders[col];

            // Skip empty headers
            if (string.IsNullOrWhiteSpace(bandHeader))
                continue;

            // Find property that matches generation, technology, and band
            var property = FindPropertyByGenerationTechnologyAndBand(properties, generation, technology, bandHeader, usedProperties);

            if (property != null)
            {
                propertyMap[col + 1] = property; // Column numbers are 1-based
                usedProperties.Add(property.Name);
            }
        }

        return propertyMap;
    }

    private static PropertyInfo? FindPropertyByGenerationTechnologyAndBand(
        PropertyInfo[] properties,
        string generation,
        string technology,
        string bandHeader,
        HashSet<string> usedProperties)
    {
        if (string.IsNullOrWhiteSpace(bandHeader))
            return null;

        // First, check for exact property name match (case insensitive) - for Provider, Barangay, etc.
        var directMatch = properties.FirstOrDefault(p =>
            string.Equals(p.Name, bandHeader, StringComparison.OrdinalIgnoreCase) &&
            !usedProperties.Contains(p.Name));

        if (directMatch != null)
            return directMatch;

        // Map technology to prefix (prioritize Technology row over Generation row)
        var technologyPrefix = string.IsNullOrEmpty(technology)
            ? MapGenerationToTechnologyPrefix(generation)
            : MapTechnologyToPrefix(technology);

        // Find property that matches the band header and hasn't been used yet
        // Prioritize properties that match the technology prefix
        var candidateProperties = properties.Where(p =>
        {
            var excelHeaderAttr = p.GetCustomAttribute<Domain.Attributes.ExcelHeaderAttribute>();
            return excelHeaderAttr != null &&
                   string.Equals(excelHeaderAttr.HeaderName, bandHeader, StringComparison.OrdinalIgnoreCase) &&
                   !usedProperties.Contains(p.Name);
        }).ToList();

        // If we have a technology prefix, try to match it first
        if (!string.IsNullOrEmpty(technologyPrefix))
        {
            var exactMatch = candidateProperties.FirstOrDefault(p =>
                p.Name.StartsWith(technologyPrefix, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
                return exactMatch;
        }

        // Otherwise, return the first unused candidate
        return candidateProperties.FirstOrDefault();
    }

    private static string MapGenerationToTechnologyPrefix(string generation)
    {
        return generation switch
        {
            "2G" => "Gsm",
            "3G" => "Wcdma",
            "4G" => "FddLte", // Default to FDD for 4G if no technology specified
            "5G" => "Nr",
            _ => string.Empty
        };
    }

    private static string MapTechnologyToPrefix(string technology)
    {
        return technology switch
        {
            "GSM" => "Gsm",
            "WCDMA" => "Wcdma",
            "FDD-LTE" => "FddLte",
            "TDD-LTE" => "TddLte",
            "NR" => "Nr",
            _ => string.Empty
        };
    }

    private static void SetPropertyValue(WirelessValidationRecord record, PropertyInfo property, string cellValue)
    {
        if (property.PropertyType == typeof(string))
        {
            property.SetValue(record, cellValue ?? string.Empty);
        }
        else if (property.PropertyType == typeof(bool))
        {
            var boolValue = ParseBooleanValue(cellValue);
            property.SetValue(record, boolValue);
        }
        else if (property.PropertyType == typeof(Guid?) || property.PropertyType == typeof(Guid))
        {
            if (Guid.TryParse(cellValue, out var guidValue))
            {
                property.SetValue(record, guidValue);
            }
            else if (property.PropertyType == typeof(Guid?))
            {
                property.SetValue(record, null);
            }
        }
        else if (!string.IsNullOrEmpty(cellValue))
        {
            var convertedValue = Convert.ChangeType(cellValue, property.PropertyType);
            property.SetValue(record, convertedValue);
        }
    }

    private static bool ParseBooleanValue(string cellValue)
    {
        if (string.IsNullOrWhiteSpace(cellValue))
            return false;

        return cellValue.Trim().ToUpperInvariant() switch
        {
            "TRUE" or "1" or "YES" or "Y" => true,
            "FALSE" or "0" or "NO" or "N" or "" => false,
            _ => false
        };
    }
}