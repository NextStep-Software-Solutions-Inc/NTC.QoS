using Microsoft.Extensions.Logging;

namespace NTC.QoS.Infrastructure.Services.Population;

/// <summary>
/// Utility class for parsing population data from Excel cells
/// </summary>
public static class PopulationDataParser
{
    /// <summary>
    /// Parses a population value from an Excel cell with comprehensive validation and logging
    /// </summary>
    /// <param name="cellValue">The raw cell value from Excel</param>
    /// <param name="fieldName">Name of the field being parsed for logging</param>
    /// <param name="locationName">Name of the location being processed for logging</param>
    /// <param name="row">Row number in the Excel sheet for logging</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Parsed population value or null if parsing fails</returns>
    public static int? ParsePopulationValue(object? cellValue, string fieldName, string locationName, int row, ILogger logger)
    {
        if (cellValue == null)
            return null;

        // Convert cell value to string first for better debugging
        var cellString = cellValue.ToString()?.Trim();
        if (string.IsNullOrEmpty(cellString))
            return null;

        try
        {
            // Handle different cell value types from ClosedXML
            int value;
            
            switch (cellValue)
            {
                case int intValue:
                    value = intValue;
                    break;
                case double doubleValue:
                    // Excel often stores numbers as doubles
                    value = (int)Math.Round(doubleValue);
                    break;
                case decimal decimalValue:
                    value = (int)Math.Round(decimalValue);
                    break;
                case string stringValue:
                    // Remove common formatting (commas, spaces)
                    var cleanString = stringValue.Replace(",", "").Replace(" ", "").Trim();
                    if (!int.TryParse(cleanString, out value))
                    {
                        logger.LogWarning("Could not parse {FieldName} string value '{OriginalValue}' (cleaned: '{CleanedValue}') for '{LocationName}' at row {Row} - setting to null", 
                            fieldName, stringValue, cleanString, locationName, row);
                        return null;
                    }
                    break;
                default:
                    // Try generic conversion as fallback
                    var fallbackString = cellString.Replace(",", "").Replace(" ", "").Trim();
                    if (!int.TryParse(fallbackString, out value))
                    {
                        logger.LogWarning("Could not parse {FieldName} value '{Value}' (type: {Type}) for '{LocationName}' at row {Row} - setting to null", 
                            fieldName, cellValue, cellValue.GetType().Name, locationName, row);
                        return null;
                    }
                    break;
            }

            // Validate the parsed value
            if (value < 0)
            {
                logger.LogWarning("Negative {FieldName} value ({Value}) for '{LocationName}' at row {Row} - setting to null", 
                    fieldName, value, locationName, row);
                return null;
            }

            // Log successful parsing for debugging
            logger.LogTrace("Successfully parsed {FieldName} value '{OriginalValue}' -> {ParsedValue} for '{LocationName}' at row {Row}", 
                fieldName, cellString, value, locationName, row);

            return value;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Exception parsing {FieldName} value '{Value}' (type: {Type}) for '{LocationName}' at row {Row} - setting to null", 
                fieldName, cellValue, cellValue?.GetType().Name ?? "null", locationName, row);
            return null;
        }
    }

    /// <summary>
    /// Parses a household count value from an Excel cell
    /// Uses the same logic as ParsePopulationValue for consistency
    /// </summary>
    /// <param name="cellValue">The raw cell value from Excel</param>
    /// <param name="fieldName">Name of the field being parsed for logging</param>
    /// <param name="locationName">Name of the location being processed for logging</param>
    /// <param name="row">Row number in the Excel sheet for logging</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Parsed household count value or null if parsing fails</returns>
    public static int? ParseHouseholdValue(object? cellValue, string fieldName, string locationName, int row, ILogger logger)
    {
        // Use the same improved logic as ParsePopulationValue
        return ParsePopulationValue(cellValue, fieldName, locationName, row, logger);
    }
}