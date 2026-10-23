using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.Style;
using System.Drawing;

namespace NTC.QoS.Infrastructure.Helper;

/// <summary>
/// Helper methods for generating Excel templates, including dropdown validation, column naming, and conditional formatting formulas.
/// </summary>
public static class ExcelTemplateHelper
{
    /// <summary>
    /// Converts a 1-indexed column number to Excel column letter notation (e.g., 1 -> "A", 27 -> "AA").
    /// </summary>
    public static string GetExcelColumnName(int columnNumber)
    {
        if (columnNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columnNumber), "Column number must be greater than zero.");
        }

        var columnName = new System.Text.StringBuilder();

        while (columnNumber > 0)
        {
            int modulo = (columnNumber - 1) % 26;
            columnName.Insert(0, Convert.ToChar('A' + modulo));
            columnNumber = (columnNumber - modulo) / 26;
        }

        return columnName.ToString();
    }

    /// <summary>
    /// Constructs the conditional formatting formula to highlight cell values that do not match the valid dropdown options.
    /// </summary>
    /// <param name="firstCellAddress">The address of the top-left cell in the validated range (e.g., "A2" or "I4").</param>
    /// <param name="sourceRangeAddress">The full absolute address of the source range (e.g., "'Sheet'!$A$1:$A$10").</param>
    /// <param name="allowFrequencyFormat">Whether to exempt frequency specifications starting with "CF:" from error highlighting.</param>
    /// <returns>The Excel formula string used in conditional formatting expressions.</returns>
    public static string BuildDropdownMismatchFormula(string firstCellAddress, string sourceRangeAddress, bool allowFrequencyFormat = false)
    {
        var conditions = new List<string>
        {
            $"{firstCellAddress}<>\"\""
        };

        if (allowFrequencyFormat)
        {
            conditions.Add($"LEFT(UPPER(TRIM({firstCellAddress})), 3)<>\"CF:\"");
        }

        conditions.Add($"ISERROR(MATCH({firstCellAddress}, {sourceRangeAddress}, 0))");
        conditions.Add($"ISERROR(MATCH(\"\"&{firstCellAddress}, {sourceRangeAddress}, 0))");

        return $"AND({string.Join(", ", conditions)})";
    }

    /// <summary>
    /// Adds dropdown list validation and mismatch conditional formatting to a target worksheet range using a source Excel range.
    /// </summary>
    public static void AddDropdownValidation(
        ExcelWorksheet worksheet,
        string range,
        ExcelRangeBase sourceRange,
        bool allowBlank = false,
        string? defaultValue = null,
        bool allowFrequencyFormat = false)
    {
        var validation = worksheet.DataValidations.AddListValidation(range);
        validation.AllowBlank = allowBlank;
        validation.Formula.ExcelFormula = sourceRange.FullAddressAbsolute;

        if (allowFrequencyFormat)
        {
            validation.ShowErrorMessage = false;
            validation.ShowInputMessage = true;
            validation.PromptTitle = "Preset Selection";
            validation.Prompt = "Please choose from the dropdown menu (1, 0, TRUE, FALSE) or enter frequency specifications (e.g., CF: <val>, BW: <val>).";
        }
        else
        {
            validation.ShowErrorMessage = true;
            validation.ErrorStyle = ExcelDataValidationWarningStyle.stop;
            validation.ErrorTitle = "Invalid Selection";
            validation.Error = "The value entered does not match any preset option. Please choose from the dropdown menu.";

            validation.ShowInputMessage = true;
            validation.PromptTitle = "Preset Selection";
            validation.Prompt = "Please choose from the dropdown menu. If copy-pasting, ensure values match the allowed options exactly.";
        }

        // Highlight pasted mismatches (turns cell light red with dark red text)
        var firstCell = range.Split(':')[0];
        var cf = worksheet.ConditionalFormatting.AddExpression(new ExcelAddress(range)); 
        cf.Formula = BuildDropdownMismatchFormula(firstCell, sourceRange.FullAddressAbsolute, allowFrequencyFormat);
        cf.Style.Fill.PatternType = ExcelFillStyle.Solid;
        cf.Style.Fill.BackgroundColor.Color = Color.FromArgb(255, 199, 206);
        cf.Style.Font.Color.Color = Color.FromArgb(156, 0, 6);

        if (!string.IsNullOrEmpty(defaultValue))
        {
            string cellAddress = range.Split(':')[0];
            ExcelRange targetCell = worksheet.Cells[cellAddress];
            targetCell.Value = defaultValue;
            targetCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }
    }

    /// <summary>
    /// Adds dropdown list validation and mismatch conditional formatting to a target worksheet range using a collection of string values.
    /// Creates a hidden lookup sheet if it does not already exist.
    /// </summary>
    public static void AddDropdownValidation(
        ExcelWorksheet worksheet,
        string range,
        IEnumerable<string> values,
        bool allowBlank = false,
        string? defaultValue = null,
        bool allowFrequencyFormat = false)
    {
        var valueList = values as IList<string> ?? values.ToList();

        // Use a hidden sheet for the values to enable both Excel Data Validation and Conditional Formatting
        var cleanRangeName = range.Replace(":", "_");
        var sheetName = "Lkp_" + cleanRangeName;
        if (sheetName.Length > 31) sheetName = sheetName[..31]; // Excel sheet names max 31 chars

        var existingSheet = worksheet.Workbook.Worksheets[sheetName];
        if (existingSheet == null)
        {
            existingSheet = worksheet.Workbook.Worksheets.Add(sheetName);
            existingSheet.Cells[1, 1].LoadFromCollection(valueList.Select(v => new { Value = v }));
            existingSheet.Hidden = eWorkSheetHidden.VeryHidden;
        }

        var sourceRange = existingSheet.Cells[1, 1, valueList.Count, 1];
        AddDropdownValidation(worksheet, range, sourceRange, allowBlank, defaultValue, allowFrequencyFormat);
    }
}
