using ClosedXML.Excel;
using NTC.QoS.Domain.Attributes;
using NTC.QoS.Domain.Models.ExcelBindings;

namespace NTC.QoS.Infrastructure.Helper;

public class CellExcelHelper
{
    public static List<CellRecord> ParseExcelToCellRecords(FileStream stream)
    {
        var result = new List<CellRecord>();

        using (var workbook = new XLWorkbook(stream))
        {
            var worksheet = workbook.Worksheet(1);

            if (worksheet == null)
            {
                return result;
            }

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

            if (lastRow < 3 || lastColumn == 0) // Ensure at least 3 header rows exist
            {
                return result;
            }

            // Get headers from the THIRD row (row index 3)
            var headers = new List<string>();
            for (int col = 1; col <= lastColumn; col++)
            {
                headers.Add(worksheet.Cell(3, col).GetString());
            }

            // Parse rows into objects, starting from the FOURTH row (row index 4) for data
            for (int row = 4; row <= lastRow; row++)
            {
                var obj = new CellRecord();
                var properties = typeof(CellRecord).GetProperties();

                for (int col = 1; col <= lastColumn; col++)
                {
                    var cellValue = worksheet.Cell(row, col).GetString();
                    if (col - 1 < headers.Count)
                    {
                        var header = headers[col - 1];

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
                                // Handle boolean conversions specifically for "TRUE"/"FALSE"
                                if (property.PropertyType == typeof(bool))
                                {
                                    if (cellValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || cellValue.Equals("1"))
                                    {
                                        property.SetValue(obj, true);
                                    }
                                    else if (cellValue.Equals("FALSE", StringComparison.OrdinalIgnoreCase) || cellValue.Equals("0") || string.IsNullOrEmpty(cellValue))
                                    {
                                        property.SetValue(obj, false);
                                    }
                                    else if (!string.IsNullOrEmpty(cellValue))
                                    {
                                        throw new InvalidOperationException($"Cannot convert value '{cellValue}' to boolean for property '{property.Name}'. Expected 'TRUE', 'FALSE', '1', or '0'.");
                                    }
                                }
                                else if (!string.IsNullOrEmpty(cellValue))
                                {
                                    var convertedValue = Convert.ChangeType(cellValue, property.PropertyType);
                                    property.SetValue(obj, convertedValue);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException($"Error converting value '{cellValue}' to type '{property.PropertyType.Name}' for property '{property.Name}'.", ex);
                            }
                        }
                    }
                }
                result.Add(obj);
            }
        }
        return result;
    }
}