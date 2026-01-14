namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Service for converting markdown content to styled PDF documents.
/// Supports full markdown syntax including tables, headers, lists, code blocks, etc.
/// </summary>
public interface IMarkdownToPdfService
{
    /// <summary>
    /// Converts markdown content to a PDF document.
    /// </summary>
    /// <param name="markdownContent">The markdown content to convert</param>
    /// <param name="title">The document title (displayed in header)</param>
    /// <param name="options">Optional PDF generation options</param>
    /// <returns>PDF document as byte array</returns>
    byte[] ConvertToPdf(string markdownContent, string title, PdfGenerationOptions? options = null);

    /// <summary>
    /// Converts markdown content to a PDF document asynchronously.
    /// </summary>
    /// <param name="markdownContent">The markdown content to convert</param>
    /// <param name="title">The document title (displayed in header)</param>
    /// <param name="options">Optional PDF generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF document as byte array</returns>
    Task<byte[]> ConvertToPdfAsync(string markdownContent, string title, PdfGenerationOptions? options = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for PDF generation from markdown
/// </summary>
public class PdfGenerationOptions
{
    /// <summary>
    /// Page orientation (portrait or landscape)
    /// </summary>
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;

    /// <summary>
    /// Include page numbers in footer
    /// </summary>
    public bool IncludePageNumbers { get; set; } = true;

    /// <summary>
    /// Include table of contents
    /// </summary>
    public bool IncludeTableOfContents { get; set; } = false;

    /// <summary>
    /// Base font size in points (default: 11)
    /// </summary>
    public float BaseFontSize { get; set; } = 11;

    /// <summary>
    /// Page margin in centimeters (default: 2)
    /// </summary>
    public float MarginCm { get; set; } = 2;

    /// <summary>
    /// Organization name to display in header (optional)
    /// </summary>
    public string? OrganizationName { get; set; }

    /// <summary>
    /// Include generation timestamp in footer
    /// </summary>
    public bool IncludeTimestamp { get; set; } = true;
}

/// <summary>
/// Page orientation for PDF documents
/// </summary>
public enum PageOrientation
{
    /// <summary>
    /// Portrait orientation (vertical)
    /// </summary>
    Portrait,

    /// <summary>
    /// Landscape orientation (horizontal)
    /// </summary>
    Landscape
}
