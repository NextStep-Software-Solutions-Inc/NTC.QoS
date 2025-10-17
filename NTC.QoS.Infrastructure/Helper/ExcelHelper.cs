using ClosedXML.Excel;
using NTC.QoS.Domain.Attributes;

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
                            var convertedValue = Convert.ChangeType(cellValue, property.PropertyType);
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
}
