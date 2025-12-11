namespace NTC.QoS.Domain.Exceptions;

/// <summary>
/// Exception thrown when Excel file parsing fails due to data validation or conversion errors.
/// </summary>
public class ExcelParsingException : Exception
{
    /// <summary>
    /// The name of the property or field that caused the parsing error.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// The row number where the error occurred (1-indexed).
    /// </summary>
    public int? RowNumber { get; }

    /// <summary>
    /// The column number where the error occurred (1-indexed).
    /// </summary>
    public int? ColumnNumber { get; }

    /// <summary>
    /// The header name of the column where the error occurred.
    /// </summary>
    public string? HeaderName { get; }

    /// <summary>
    /// The problematic value that could not be parsed.
    /// </summary>
    public string? Value { get; }

    public ExcelParsingException(string message) : base(message)
    {
    }

    public ExcelParsingException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    public ExcelParsingException(
        string message,
        string? propertyName = null,
        int? rowNumber = null,
        int? columnNumber = null,
        string? headerName = null,
        string? value = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        PropertyName = propertyName;
        RowNumber = rowNumber;
        ColumnNumber = columnNumber;
        HeaderName = headerName;
        Value = value;
    }
}
