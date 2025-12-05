using ClosedXML.Excel;
using NTC.QoS.Domain.Attributes;
using System.Globalization;

namespace NTC.QoS.Infrastructure.Helper;

public static class ExcelHelper
{
    /// <summary>
    /// Parses Excel data from a Stream (supports both FileStream and blob storage streams)
    /// </summary>
    public static List<T> ParseExcelToClass<T>(Stream stream) where T : new()
    {
        var result = new List<T>();

        using (var workbook = new XLWorkbook(stream))
        {
            var worksheet = workbook.Worksheet(1); // Use the first worksheet

            if (worksheet == null)
            {
                return result;
            }

            var lastRow = worksheet.LastRowUsed();
            var lastColumn = worksheet.LastColumnUsed();

            if (lastRow == null || lastColumn == null)
            {
                return result;
            }

            var rowCount = lastRow.RowNumber();
            var columnCount = lastColumn.ColumnNumber();

            // STEP 1: Get headers from the first row
            var headers = new List<string>();
            for (int col = 1; col <= columnCount; col++)
            {
                headers.Add(worksheet.Cell(1, col).GetString());
            }

            // STEP 2: Parse rows into objects
            for (int row = 2; row <= rowCount; row++) // Start from row 2 (skip header)
            {
                var obj = new T(); // Create new SpeedtestRecord
                var properties = typeof(T).GetProperties(); // Get all properties

                for (int col = 1; col <= columnCount; col++)
                {
                    var cellValue = worksheet.Cell(row, col).GetString();
                    var header = headers[col - 1];

                    // STEP 3: Match header to property using ExcelHeaderAttribute
                    var property = properties.FirstOrDefault(p =>
                    {
                        var attribute = p.GetCustomAttributes(typeof(ExcelHeaderAttribute), false)
                                         .Cast<ExcelHeaderAttribute>()
                                         .FirstOrDefault();
                        return attribute != null && attribute.HeaderName.Equals(header, StringComparison.OrdinalIgnoreCase);
                    });

                    if (property != null && property.CanWrite)
                    {
                        try
                        {
                            // STEP 4: Convert string to property type
                            object? convertedValue;
                            
                            // Special handling for DateTime to support multiple formats
                            if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                            {
                                convertedValue = ParseDateTime(cellValue);
                            }
                            else
                            {
                                convertedValue = Convert.ChangeType(cellValue, property.PropertyType);
                            }
                            
                            property.SetValue(obj, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            // Only log the problematic row, not all rows
                            Console.WriteLine($"[ExcelHelper][ERROR] Row: {row}, Col: {col}, Header: '{header}', Property: '{property.Name}', Value: '{cellValue}', Type: '{property.PropertyType.Name}', Exception: {ex.Message}");
                            throw new InvalidOperationException($"Error converting value '{cellValue}' to type '{property.PropertyType.Name}' for property '{property.Name}' at Row {row}, Col {col} (Header: '{header}').", ex);
                        }
                    }
                }

                result.Add(obj); // Add completed object to list
            }
        }

        return result;
    }

    /// <summary>
    /// Legacy method for backward compatibility with FileStream
    /// </summary>
    [Obsolete("Use ParseExcelToClass<T>(Stream) instead. This method will be removed in a future version.")]
    public static List<T> ParseExcelToClass<T>(FileStream stream) where T : new()
    {
        return ParseExcelToClass<T>((Stream)stream);
    }

    /// <summary>
    /// Parses DateTime from string with support for multiple formats
    /// </summary>
    private static DateTime ParseDateTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException("DateTime value cannot be empty");
        }

        // Try parsing with multiple datetime formats
        var formats = new[]
        {
            "dd/MM/yyyy, HH:mm",      // 26/09/2025, 08:46
            "dd/MM/yyyy HH:mm",        // 26/09/2025 08:46
            "dd/MM/yyyy, HH:mm:ss",    // 26/09/2025, 08:46:30
            "dd/MM/yyyy HH:mm:ss",     // 26/09/2025 08:46:30
            "dd/MM/yyyy",              // 26/09/2025
            "MM/dd/yyyy, HH:mm",       // 09/26/2025, 08:46
            "MM/dd/yyyy HH:mm",        // 09/26/2025 08:46
            "MM/dd/yyyy, HH:mm:ss",    // 09/26/2025, 08:46:30
            "MM/dd/yyyy HH:mm:ss",     // 09/26/2025 08:46:30
            "MM/dd/yyyy",              // 09/26/2025
            "yyyy-MM-dd HH:mm:ss",     // 2025-09-26 08:46:30
            "yyyy-MM-dd HH:mm",        // 2025-09-26 08:46
            "yyyy-MM-dd",              // 2025-09-26
        };

        // Try parsing with known formats
        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
        {
            return result;
        }

        // Fallback to standard DateTime parsing
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            return result;
        }

        throw new FormatException($"Unable to parse '{value}' as DateTime. Supported formats include: dd/MM/yyyy, HH:mm");
    }
}
