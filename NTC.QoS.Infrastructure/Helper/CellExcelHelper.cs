using ClosedXML.Excel;
using NTC.QoS.Domain.Attributes;
using NTC.QoS.Domain.Models.ExcelBindings;
using System.Reflection;

namespace NTC.QoS.Infrastructure.Helper;

public class CellExcelHelper
{
    /// <summary>
    /// Parses Excel data from a Stream (supports both FileStream and blob storage streams)
    /// </summary>
    public static List<CellRecord> ParseExcelToCellRecords(Stream stream)
    {
        var result = new List<CellRecord>();

        using (var workbook = new XLWorkbook(stream))
        {
            var worksheet = workbook.Worksheet(1);

            if (worksheet == null)
                return result;

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

            if (lastRow < 3 || lastColumn == 0)
                return result;

            // Read headers from row 3
            var headers = new List<string>();
            for (int col = 1; col <= lastColumn; col++)
            {
                headers.Add(worksheet.Cell(3, col).GetString());
            }

            var properties = typeof(CellRecord).GetProperties();

            // Get a map of column index → property based on property order
            // Assumes that Excel columns are in the same order as properties
            var propertyMap = new Dictionary<int, PropertyInfo>();
            int propertyIndex = 0;
            for (int col = 1; col <= lastColumn && propertyIndex < properties.Length; col++)
            {
                var property = properties[propertyIndex];
                propertyMap[col] = property;
                propertyIndex++;
            }

            for (int row = 4; row <= lastRow; row++)
            {
                var obj = new CellRecord();

                for (int col = 1; col <= lastColumn; col++)
                {
                    var cellValue = worksheet.Cell(row, col).GetString();

                    if (propertyMap.TryGetValue(col, out var property) && property.CanWrite)
                    {
                        try
                        {
                            // Handle string properties (including band/technology columns)
                            if (property.PropertyType == typeof(string))
                            {
                                property.SetValue(obj, cellValue ?? string.Empty);
                            }
                            // Handle legacy bool parsing (for backward compatibility)
                            else if (property.PropertyType == typeof(bool))
                            {
                                if (cellValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || cellValue == "1")
                                    property.SetValue(obj, true);
                                else if (cellValue.Equals("FALSE", StringComparison.OrdinalIgnoreCase) || cellValue == "0" || string.IsNullOrEmpty(cellValue))
                                    property.SetValue(obj, false);
                                else
                                    throw new InvalidOperationException($"Cannot convert value '{cellValue}' to boolean for '{property.Name}'");
                            }
                            else if (!string.IsNullOrEmpty(cellValue))
                            {
                                var convertedValue = Convert.ChangeType(cellValue, property.PropertyType);
                                property.SetValue(obj, convertedValue);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Error converting value '{cellValue}' to type '{property.PropertyType.Name}' for property '{property.Name}' at row {row}, column {col}.", ex);
                        }
                    }
                }

                result.Add(obj);
            }
        }

        return result;
    }

    /// <summary>
    /// Legacy method for backward compatibility with FileStream
    /// </summary>
    [Obsolete("Use ParseExcelToCellRecords(Stream) instead. This method will be removed in a future version.")]
    public static List<CellRecord> ParseExcelToCellRecords(FileStream stream)
    {
        return ParseExcelToCellRecords((Stream)stream);
    }
}