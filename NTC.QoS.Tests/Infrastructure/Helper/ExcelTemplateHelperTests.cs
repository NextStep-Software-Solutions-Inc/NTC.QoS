using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NTC.QoS.Infrastructure.Helper;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting.Contracts;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.Style;
using Xunit;

namespace NTC.QoS.Tests.Infrastructure.Helper;

public class ExcelTemplateHelperTests
{
    [Theory]
    [InlineData(1, "A")]
    [InlineData(2, "B")]
    [InlineData(26, "Z")]
    [InlineData(27, "AA")]
    [InlineData(28, "AB")]
    [InlineData(52, "AZ")]
    [InlineData(53, "BA")]
    [InlineData(702, "ZZ")]
    [InlineData(703, "AAA")]
    public void GetExcelColumnName_ValidColumnNumbers_ReturnsExpectedLetterNotation(int columnNumber, string expected)
    {
        var result = ExcelTemplateHelper.GetExcelColumnName(columnNumber);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void GetExcelColumnName_ZeroOrNegative_ThrowsArgumentOutOfRangeException(int columnNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ExcelTemplateHelper.GetExcelColumnName(columnNumber));
    }

    [Fact]
    public void BuildDropdownMismatchFormula_Standard_ReturnsExpectedFormula()
    {
        var firstCell = "A2";
        var sourceRange = "Providers!$A$1:$A$10";

        var formula = ExcelTemplateHelper.BuildDropdownMismatchFormula(firstCell, sourceRange, allowFrequencyFormat: false);

        Assert.Equal("AND(A2<>\"\", ISERROR(MATCH(A2, Providers!$A$1:$A$10, 0)), ISERROR(MATCH(\"\"&A2, Providers!$A$1:$A$10, 0)))", formula);
    }

    [Fact]
    public void BuildDropdownMismatchFormula_WithFrequencyFormat_IncludesExemptionCondition()
    {
        var firstCell = "I4";
        var sourceRange = "BandValues!$A$1:$A$4";

        var formula = ExcelTemplateHelper.BuildDropdownMismatchFormula(firstCell, sourceRange, allowFrequencyFormat: true);

        Assert.Equal("AND(I4<>\"\", LEFT(UPPER(TRIM(I4)), 3)<>\"CF:\", ISERROR(MATCH(I4, BandValues!$A$1:$A$4, 0)), ISERROR(MATCH(\"\"&I4, BandValues!$A$1:$A$4, 0)))", formula);
    }

    [Fact]
    public void AddDropdownValidation_StandardSourceRange_ConfiguresValidationAndConditionalFormatting()
    {
        ExcelPackage.License.SetNonCommercialPersonal("Test");
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Main");
        var sourceWs = package.Workbook.Worksheets.Add("Source");
        sourceWs.Cells["A1"].Value = "Option1";
        sourceWs.Cells["A2"].Value = "Option2";
        var sourceRange = sourceWs.Cells["A1:A2"];

        ExcelTemplateHelper.AddDropdownValidation(ws, "B2:B10", sourceRange, allowBlank: false, defaultValue: "Option1");

        // Validate List Validation
        Assert.Single(ws.DataValidations);
        var validation = Assert.IsAssignableFrom<ExcelDataValidationList>(ws.DataValidations.First());
        Assert.False(validation.AllowBlank);
        Assert.True(validation.ShowErrorMessage);
        Assert.Equal(ExcelDataValidationWarningStyle.stop, validation.ErrorStyle);
        Assert.Equal("Invalid Selection", validation.ErrorTitle);
        Assert.Equal(sourceRange.FullAddressAbsolute, validation.Formula.ExcelFormula);

        // Validate Default Value
        Assert.Equal("Option1", ws.Cells["B2"].Value);
        Assert.Equal(ExcelHorizontalAlignment.Center, ws.Cells["B2"].Style.HorizontalAlignment);

        // Validate Conditional Formatting
        Assert.Single(ws.ConditionalFormatting);
        var cf = Assert.IsAssignableFrom<IExcelConditionalFormattingExpression>(ws.ConditionalFormatting.First());
        var expectedFormula = ExcelTemplateHelper.BuildDropdownMismatchFormula("B2", sourceRange.FullAddressAbsolute, allowFrequencyFormat: false);
        Assert.Equal(expectedFormula, cf.Formula);
        Assert.Equal(ExcelFillStyle.Solid, cf.Style.Fill.PatternType);
        Assert.Equal(Color.FromArgb(255, 199, 206).ToArgb(), cf.Style.Fill.BackgroundColor.Color?.ToArgb());
        Assert.Equal(Color.FromArgb(156, 0, 6).ToArgb(), cf.Style.Font.Color.Color?.ToArgb());
    }

    [Fact]
    public void AddDropdownValidation_AllowFrequencyFormat_SetsNonStrictWarningAndExemptionFormula()
    {
        ExcelPackage.License.SetNonCommercialPersonal("Test");
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Main");
        var sourceWs = package.Workbook.Worksheets.Add("Source");
        sourceWs.Cells["A1"].Value = "1";
        sourceWs.Cells["A2"].Value = "0";
        var sourceRange = sourceWs.Cells["A1:A2"];

        ExcelTemplateHelper.AddDropdownValidation(ws, "I4:I100", sourceRange, allowBlank: true, allowFrequencyFormat: true);

        // Validate List Validation
        Assert.Single(ws.DataValidations);
        var validation = Assert.IsAssignableFrom<ExcelDataValidationList>(ws.DataValidations.First());
        Assert.True(validation.AllowBlank);
        Assert.False(validation.ShowErrorMessage);
        Assert.True(validation.ShowInputMessage);
        Assert.Contains("CF:", validation.Prompt);

        // Validate Conditional Formatting Exemption
        Assert.Single(ws.ConditionalFormatting);
        var cf = Assert.IsAssignableFrom<IExcelConditionalFormattingExpression>(ws.ConditionalFormatting.First());
        var expectedFormula = ExcelTemplateHelper.BuildDropdownMismatchFormula("I4", sourceRange.FullAddressAbsolute, allowFrequencyFormat: true);
        Assert.Equal(expectedFormula, cf.Formula);
        Assert.Contains("CF:", cf.Formula);
    }

    [Fact]
    public void AddDropdownValidation_WithValuesCollection_CreatesHiddenLookupSheet()
    {
        ExcelPackage.License.SetNonCommercialPersonal("Test");
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Main");
        var values = new List<string> { "NBC", "TRUE", "FALSE" };

        ExcelTemplateHelper.AddDropdownValidation(ws, "X2:X100", values, allowBlank: true);

        // Check lookup sheet created and hidden
        var lookupSheet = package.Workbook.Worksheets["Lkp_X2_X100"];
        Assert.NotNull(lookupSheet);
        Assert.Equal(eWorkSheetHidden.VeryHidden, lookupSheet.Hidden);
        Assert.Equal("NBC", lookupSheet.Cells[1, 1].Value);
        Assert.Equal("TRUE", lookupSheet.Cells[2, 1].Value);
        Assert.Equal("FALSE", lookupSheet.Cells[3, 1].Value);

        // Validation & Conditional Formatting present on Main worksheet
        Assert.Single(ws.DataValidations);
        Assert.Single(ws.ConditionalFormatting);
    }
}
