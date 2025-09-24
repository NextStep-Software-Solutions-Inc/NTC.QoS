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
            var municipalityInfo = ParseMunicipalityFromA1(worksheet.Cell(1, 1).GetString());

            // Read headers from row 4
            var headers = new List<string>();
            for (int col = 1; col <= lastColumn; col++)
            {
                headers.Add(worksheet.Cell(4, col).GetString());
            }

            // Create property mapping based on headers
            var properties = typeof(WirelessValidationRecord).GetProperties();
            var propertyMap = CreatePropertyMapping(headers, properties);

            // Process data rows starting from row 5 (after headers in row 4)
            for (int row = 5; row <= lastRow; row++)
            {
                var record = new WirelessValidationRecord
                {
                    // Set municipality info parsed from A1
                    Municipality = municipalityInfo.Municipality,
                    Province = municipalityInfo.Province,
                    Region = municipalityInfo.Region
                };

                for (int col = 1; col <= lastColumn; col++)
                {
                    var cellValue = worksheet.Cell(row, col).GetString();
                    var header = col <= headers.Count ? headers[col - 1] : string.Empty;

                    if (propertyMap.TryGetValue(col, out var property) && property.CanWrite)
                    {
                        try
                        {
                            SetPropertyValue(record, property, cellValue, header);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Error converting value '{cellValue}' to type '{property.PropertyType.Name}' for property '{property.Name}' at row {row}, column {col} (header: '{header}').", ex);
                        }
                    }
                }

                // Construct the complete address from barangay, municipality, province, region
                if (!string.IsNullOrWhiteSpace(record.Barangay))
                {
                    record.Address = $"{record.Barangay}, {record.Municipality}, {record.Province}, {record.Region}";
                }

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

    private static Dictionary<int, PropertyInfo> CreatePropertyMapping(List<string> headers, PropertyInfo[] properties)
    {
        var propertyMap = new Dictionary<int, PropertyInfo>();

        // Map standard fields first (Provider, Barangay, etc.)
        for (int col = 0; col < headers.Count; col++)
        {
            var header = headers[col];
            var property = FindPropertyByHeader(properties, header);
            if (property != null)
            {
                propertyMap[col + 1] = property; // Column numbers are 1-based
            }
        }

        return propertyMap;
    }

    private static PropertyInfo? FindPropertyByHeader(PropertyInfo[] properties, string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return null;

        // First, check for exact property name match (case insensitive)
        var directMatch = properties.FirstOrDefault(p => 
            string.Equals(p.Name, header, StringComparison.OrdinalIgnoreCase));
        if (directMatch != null)
            return directMatch;

        // Then check for ExcelHeader attribute matches
        var attributeMatch = properties.FirstOrDefault(p =>
            {
                var excelHeaderAttr = p.GetCustomAttribute<Domain.Attributes.ExcelHeaderAttribute>();
                return excelHeaderAttr != null && 
                       string.Equals(excelHeaderAttr.HeaderName, header, StringComparison.OrdinalIgnoreCase);
            });

        return attributeMatch;
    }

    private static void SetPropertyValue(WirelessValidationRecord record, PropertyInfo property, string cellValue, string header)
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
