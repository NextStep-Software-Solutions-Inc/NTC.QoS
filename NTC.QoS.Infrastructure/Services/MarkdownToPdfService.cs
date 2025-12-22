using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.RegularExpressions;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Service for converting markdown content to beautifully styled PDF documents.
/// Supports comprehensive markdown syntax including:
/// - Headers (h1-h6)
/// - Tables with proper formatting
/// - Bullet and numbered lists (including nested)
/// - Code blocks (inline and fenced)
/// - Bold, italic, and strikethrough text
/// - Blockquotes
/// - Horizontal rules
/// - Links (rendered as text)
/// </summary>
public class MarkdownToPdfService : IMarkdownToPdfService
{
    private readonly ILogger<MarkdownToPdfService> _logger;

    // Regex patterns for markdown parsing
    private static readonly Regex HeaderPattern = new(@"^(#{1,6})\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex BulletListPattern = new(@"^(\s*)([-*+])\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex NumberedListPattern = new(@"^(\s*)(\d+)\.\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex TableRowPattern = new(@"^\|(.+)\|$", RegexOptions.Compiled);
    private static readonly Regex TableSeparatorPattern = new(@"^\|[\s\-:|]+\|$", RegexOptions.Compiled);
    private static readonly Regex BlockquotePattern = new(@"^>\s*(.*)$", RegexOptions.Compiled);
    private static readonly Regex HorizontalRulePattern = new(@"^[\-*_]{3,}$", RegexOptions.Compiled);
    private static readonly Regex FencedCodeBlockStartPattern = new(@"^```(\w*)$", RegexOptions.Compiled);
    private static readonly Regex FencedCodeBlockEndPattern = new(@"^```$", RegexOptions.Compiled);
    private static readonly Regex BoldPattern = new(@"\*\*(.+?)\*\*|__(.+?)__", RegexOptions.Compiled);
    private static readonly Regex ItalicPattern = new(@"\*(.+?)\*|_(.+?)_", RegexOptions.Compiled);
    private static readonly Regex InlineCodePattern = new(@"`([^`]+)`", RegexOptions.Compiled);
    private static readonly Regex StrikethroughPattern = new(@"~~(.+?)~~", RegexOptions.Compiled);
    private static readonly Regex LinkPattern = new(@"\[([^\]]+)\]\(([^\)]+)\)", RegexOptions.Compiled);

    public MarkdownToPdfService(ILogger<MarkdownToPdfService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public byte[] ConvertToPdf(string markdownContent, string title, PdfGenerationOptions? options = null)
    {
        options ??= new PdfGenerationOptions();
        
        _logger.LogInformation("Converting markdown to PDF. Title: {Title}, Content length: {Length}", 
            title, markdownContent?.Length ?? 0);

        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = CreateDocument(markdownContent ?? string.Empty, title, options);
            var pdfBytes = document.GeneratePdf();

            _logger.LogInformation("Successfully generated PDF. Size: {Size} bytes", pdfBytes.Length);
            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert markdown to PDF");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<byte[]> ConvertToPdfAsync(string markdownContent, string title, PdfGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ConvertToPdf(markdownContent, title, options), cancellationToken);
    }

    private IDocument CreateDocument(string markdownContent, string title, PdfGenerationOptions options)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                // Page setup
                var pageSize = options.Orientation == PageOrientation.Landscape 
                    ? PageSizes.A4.Landscape() 
                    : PageSizes.A4;
                
                page.Size(pageSize);
                page.Margin(options.MarginCm, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(options.BaseFontSize));

                // Header
                page.Header().Element(c => ComposeHeader(c, title, options));

                // Content
                page.Content()
                    .PaddingVertical(0.5f, Unit.Centimetre)
                    .Element(c => ComposeContent(c, markdownContent, options));

                // Footer
                page.Footer().Element(c => ComposeFooter(c, options));
            });
        });
    }

    private void ComposeHeader(IContainer container, string title, PdfGenerationOptions options)
    {
        container.Column(column =>
        {
            // Organization name (if provided)
            if (!string.IsNullOrWhiteSpace(options.OrganizationName))
            {
                column.Item().AlignCenter()
                    .Text(options.OrganizationName)
                    .FontSize(options.BaseFontSize + 2)
                    .FontColor(Colors.Grey.Darken2);
            }

            // Document title
            column.Item()
                .PaddingBottom(0.3f, Unit.Centimetre)
                .BorderBottom(1)
                .BorderColor(Colors.Blue.Medium)
                .Text(title)
                .SemiBold()
                .FontSize(options.BaseFontSize + 7)
                .FontColor(Colors.Blue.Darken2);
        });
    }

    private void ComposeFooter(IContainer container, PdfGenerationOptions options)
    {
        container.Row(row =>
        {
            if (options.IncludeTimestamp)
            {
                row.RelativeItem().AlignLeft()
                    .Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                    .FontSize(options.BaseFontSize - 2)
                    .FontColor(Colors.Grey.Medium);
            }
            else
            {
                row.RelativeItem();
            }

            if (options.IncludePageNumbers)
            {
                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Page ").FontSize(options.BaseFontSize - 2).FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(options.BaseFontSize - 2).FontColor(Colors.Grey.Medium);
                    x.Span(" of ").FontSize(options.BaseFontSize - 2).FontColor(Colors.Grey.Medium);
                    x.TotalPages().FontSize(options.BaseFontSize - 2).FontColor(Colors.Grey.Medium);
                });
            }
        });
    }

    private void ComposeContent(IContainer container, string markdownContent, PdfGenerationOptions options)
    {
        container.Column(column =>
        {
            column.Spacing(4);

            if (string.IsNullOrWhiteSpace(markdownContent))
            {
                column.Item().Text("No content available.");
                return;
            }

            var lines = markdownContent.Split('\n');
            var elements = ParseMarkdownElements(lines);

            foreach (var element in elements)
            {
                RenderElement(column, element, options);
            }
        });
    }

    private List<MarkdownElement> ParseMarkdownElements(string[] lines)
    {
        var elements = new List<MarkdownElement>();
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];
            var trimmedLine = line.TrimEnd();

            // Empty line
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                elements.Add(new MarkdownElement { Type = ElementType.BlankLine });
                i++;
                continue;
            }

            // Fenced code block
            var codeMatch = FencedCodeBlockStartPattern.Match(trimmedLine);
            if (codeMatch.Success)
            {
                var language = codeMatch.Groups[1].Value;
                var codeLines = new List<string>();
                i++;

                while (i < lines.Length && !FencedCodeBlockEndPattern.IsMatch(lines[i].TrimEnd()))
                {
                    codeLines.Add(lines[i]);
                    i++;
                }

                elements.Add(new MarkdownElement
                {
                    Type = ElementType.CodeBlock,
                    Content = string.Join("\n", codeLines),
                    Metadata = language
                });

                i++; // Skip closing ```
                continue;
            }

            // Horizontal rule
            if (HorizontalRulePattern.IsMatch(trimmedLine))
            {
                elements.Add(new MarkdownElement { Type = ElementType.HorizontalRule });
                i++;
                continue;
            }

            // Header
            var headerMatch = HeaderPattern.Match(trimmedLine);
            if (headerMatch.Success)
            {
                elements.Add(new MarkdownElement
                {
                    Type = ElementType.Header,
                    Content = headerMatch.Groups[2].Value,
                    Level = headerMatch.Groups[1].Value.Length
                });
                i++;
                continue;
            }

            // Table
            if (TableRowPattern.IsMatch(trimmedLine))
            {
                var tableRows = new List<string[]>();
                var hasHeader = false;

                // Collect all table rows
                while (i < lines.Length && TableRowPattern.IsMatch(lines[i].TrimEnd()))
                {
                    var rowLine = lines[i].TrimEnd();
                    
                    // Check if this is the separator row
                    if (TableSeparatorPattern.IsMatch(rowLine))
                    {
                        hasHeader = true;
                        i++;
                        continue;
                    }

                    var cells = ParseTableRow(rowLine);
                    tableRows.Add(cells);
                    i++;
                }

                elements.Add(new MarkdownElement
                {
                    Type = ElementType.Table,
                    TableRows = tableRows,
                    Metadata = hasHeader ? "header" : null
                });
                continue;
            }

            // Blockquote
            var blockquoteMatch = BlockquotePattern.Match(trimmedLine);
            if (blockquoteMatch.Success)
            {
                var quoteLines = new List<string>();
                
                while (i < lines.Length)
                {
                    var qMatch = BlockquotePattern.Match(lines[i].TrimEnd());
                    if (qMatch.Success)
                    {
                        quoteLines.Add(qMatch.Groups[1].Value);
                        i++;
                    }
                    else if (string.IsNullOrWhiteSpace(lines[i]))
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }

                elements.Add(new MarkdownElement
                {
                    Type = ElementType.Blockquote,
                    Content = string.Join("\n", quoteLines)
                });
                continue;
            }

            // Bullet list
            var bulletMatch = BulletListPattern.Match(trimmedLine);
            if (bulletMatch.Success)
            {
                var listItems = new List<(int indent, string content)>();
                
                while (i < lines.Length)
                {
                    var bMatch = BulletListPattern.Match(lines[i]);
                    if (bMatch.Success)
                    {
                        var indent = bMatch.Groups[1].Value.Length;
                        listItems.Add((indent, bMatch.Groups[3].Value));
                        i++;
                    }
                    else if (string.IsNullOrWhiteSpace(lines[i]))
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }

                elements.Add(new MarkdownElement
                {
                    Type = ElementType.BulletList,
                    ListItems = listItems
                });
                continue;
            }

            // Numbered list
            var numberedMatch = NumberedListPattern.Match(trimmedLine);
            if (numberedMatch.Success)
            {
                var listItems = new List<(int indent, string content)>();
                
                while (i < lines.Length)
                {
                    var nMatch = NumberedListPattern.Match(lines[i]);
                    if (nMatch.Success)
                    {
                        var indent = nMatch.Groups[1].Value.Length;
                        listItems.Add((indent, nMatch.Groups[3].Value));
                        i++;
                    }
                    else if (string.IsNullOrWhiteSpace(lines[i]))
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }

                elements.Add(new MarkdownElement
                {
                    Type = ElementType.NumberedList,
                    ListItems = listItems
                });
                continue;
            }

            // Regular paragraph
            var paragraphLines = new List<string>();
            while (i < lines.Length && 
                   !string.IsNullOrWhiteSpace(lines[i]) && 
                   !HeaderPattern.IsMatch(lines[i].TrimEnd()) &&
                   !BulletListPattern.IsMatch(lines[i]) &&
                   !NumberedListPattern.IsMatch(lines[i]) &&
                   !TableRowPattern.IsMatch(lines[i].TrimEnd()) &&
                   !BlockquotePattern.IsMatch(lines[i].TrimEnd()) &&
                   !HorizontalRulePattern.IsMatch(lines[i].TrimEnd()) &&
                   !FencedCodeBlockStartPattern.IsMatch(lines[i].TrimEnd()))
            {
                paragraphLines.Add(lines[i].Trim());
                i++;
            }

            if (paragraphLines.Count > 0)
            {
                elements.Add(new MarkdownElement
                {
                    Type = ElementType.Paragraph,
                    Content = string.Join(" ", paragraphLines)
                });
            }
        }

        return elements;
    }

    private static string[] ParseTableRow(string row)
    {
        // Remove leading and trailing pipes
        var content = row.Trim('|');
        return content.Split('|').Select(c => c.Trim()).ToArray();
    }

    private void RenderElement(ColumnDescriptor column, MarkdownElement element, PdfGenerationOptions options)
    {
        switch (element.Type)
        {
            case ElementType.BlankLine:
                column.Item().Height(8);
                break;

            case ElementType.Header:
                RenderHeader(column, element, options);
                break;

            case ElementType.Paragraph:
                RenderParagraph(column, element.Content ?? "", options);
                break;

            case ElementType.BulletList:
                RenderBulletList(column, element, options);
                break;

            case ElementType.NumberedList:
                RenderNumberedList(column, element, options);
                break;

            case ElementType.Table:
                RenderTable(column, element, options);
                break;

            case ElementType.CodeBlock:
                RenderCodeBlock(column, element, options);
                break;

            case ElementType.Blockquote:
                RenderBlockquote(column, element, options);
                break;

            case ElementType.HorizontalRule:
                column.Item()
                    .PaddingVertical(8)
                    .LineHorizontal(1)
                    .LineColor(Colors.Grey.Lighten1);
                break;
        }
    }

    private void RenderHeader(ColumnDescriptor column, MarkdownElement element, PdfGenerationOptions options)
    {
        var fontSize = element.Level switch
        {
            1 => options.BaseFontSize + 9,
            2 => options.BaseFontSize + 6,
            3 => options.BaseFontSize + 4,
            4 => options.BaseFontSize + 2,
            5 => options.BaseFontSize + 1,
            _ => options.BaseFontSize
        };

        var fontColor = element.Level switch
        {
            1 => Colors.Blue.Darken3,
            2 => Colors.Blue.Darken2,
            3 => Colors.Blue.Darken1,
            _ => Colors.Grey.Darken3
        };

        column.Item()
            .PaddingTop(element.Level <= 2 ? 12 : 8)
            .PaddingBottom(4)
            .Text(text =>
            {
                var cleanedContent = CleanInlineFormatting(element.Content ?? "");
                text.Span(cleanedContent)
                    .FontSize(fontSize)
                    .SemiBold()
                    .FontColor(fontColor);
            });

        // Add underline for h1 and h2
        if (element.Level <= 2)
        {
            column.Item()
                .PaddingBottom(4)
                .LineHorizontal(element.Level == 1 ? 2 : 1)
                .LineColor(Colors.Blue.Lighten2);
        }
    }

    private void RenderParagraph(ColumnDescriptor column, string content, PdfGenerationOptions options)
    {
        column.Item().Text(text =>
        {
            RenderFormattedText(text, content, options);
        });
    }

    private void RenderFormattedText(TextDescriptor text, string content, PdfGenerationOptions options)
    {
        // Process inline formatting
        var segments = ParseInlineFormatting(content);
        
        foreach (var segment in segments)
        {
            var span = text.Span(segment.Text);
            
            if (segment.IsBold)
                span = span.Bold();
            if (segment.IsItalic)
                span = span.Italic();
            if (segment.IsCode)
            {
                span = span.FontColor(Colors.Red.Darken2)
                           .FontSize(options.BaseFontSize - 1);
            }
            if (segment.IsStrikethrough)
                span = span.Strikethrough();
        }
    }

    private static string CleanInlineFormatting(string content)
    {
        // Remove markdown formatting for plain text extraction
        var result = content;
        result = BoldPattern.Replace(result, "$1$2");
        result = ItalicPattern.Replace(result, "$1$2");
        result = InlineCodePattern.Replace(result, "$1");
        result = StrikethroughPattern.Replace(result, "$1");
        result = LinkPattern.Replace(result, "$1");
        return result;
    }

    private List<TextSegment> ParseInlineFormatting(string content)
    {
        var segments = new List<TextSegment>();
        var currentPos = 0;

        // Simplified inline parsing - handles common cases
        var patterns = new (Regex pattern, Action<Match, List<TextSegment>> handler)[]
        {
            (BoldPattern, (m, s) => s.Add(new TextSegment { Text = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value, IsBold = true })),
            (ItalicPattern, (m, s) => s.Add(new TextSegment { Text = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value, IsItalic = true })),
            (InlineCodePattern, (m, s) => s.Add(new TextSegment { Text = m.Groups[1].Value, IsCode = true })),
            (StrikethroughPattern, (m, s) => s.Add(new TextSegment { Text = m.Groups[1].Value, IsStrikethrough = true })),
            (LinkPattern, (m, s) => s.Add(new TextSegment { Text = m.Groups[1].Value }))
        };

        // Simple approach: find all matches and sort by position
        var allMatches = new List<(int start, int end, TextSegment segment)>();
        
        foreach (var (pattern, _) in patterns)
        {
            foreach (Match match in pattern.Matches(content))
            {
                TextSegment segment;
                if (pattern == BoldPattern)
                    segment = new TextSegment { Text = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value, IsBold = true };
                else if (pattern == ItalicPattern)
                    segment = new TextSegment { Text = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value, IsItalic = true };
                else if (pattern == InlineCodePattern)
                    segment = new TextSegment { Text = match.Groups[1].Value, IsCode = true };
                else if (pattern == StrikethroughPattern)
                    segment = new TextSegment { Text = match.Groups[1].Value, IsStrikethrough = true };
                else // Link
                    segment = new TextSegment { Text = match.Groups[1].Value };
                
                allMatches.Add((match.Index, match.Index + match.Length, segment));
            }
        }

        // Sort matches by position
        allMatches = allMatches.OrderBy(m => m.start).ToList();

        // Build segments, handling overlapping matches by taking the first one
        foreach (var (start, end, segment) in allMatches)
        {
            if (start < currentPos)
                continue; // Skip overlapping match

            // Add plain text before this match
            if (start > currentPos)
            {
                segments.Add(new TextSegment { Text = content.Substring(currentPos, start - currentPos) });
            }

            segments.Add(segment);
            currentPos = end;
        }

        // Add remaining plain text
        if (currentPos < content.Length)
        {
            segments.Add(new TextSegment { Text = content.Substring(currentPos) });
        }

        // If no formatting was found, return the whole content as plain text
        if (segments.Count == 0)
        {
            segments.Add(new TextSegment { Text = content });
        }

        return segments;
    }

    private void RenderBulletList(ColumnDescriptor column, MarkdownElement element, PdfGenerationOptions options)
    {
        if (element.ListItems == null) return;

        column.Item().Column(listColumn =>
        {
            listColumn.Spacing(3);
            
            foreach (var (indent, content) in element.ListItems)
            {
                var indentLevel = indent / 2; // Approximate indentation level
                var bulletChar = indentLevel switch
                {
                    0 => "•",
                    1 => "◦",
                    _ => "▪"
                };

                listColumn.Item().Row(row =>
                {
                    row.ConstantItem(15 + (indentLevel * 15)); // Base indent + additional per level
                    row.ConstantItem(15).Text(bulletChar)
                        .FontSize(options.BaseFontSize - 1)
                        .FontColor(Colors.Grey.Darken1);
                    row.RelativeItem().Text(text =>
                    {
                        RenderFormattedText(text, content, options);
                    });
                });
            }
        });
    }

    private void RenderNumberedList(ColumnDescriptor column, MarkdownElement element, PdfGenerationOptions options)
    {
        if (element.ListItems == null) return;

        column.Item().Column(listColumn =>
        {
            listColumn.Spacing(3);
            var itemNumber = 1;

            foreach (var (indent, content) in element.ListItems)
            {
                var indentLevel = indent / 2;
                
                listColumn.Item().Row(row =>
                {
                    row.ConstantItem(15 + (indentLevel * 15));
                    row.ConstantItem(20).Text($"{itemNumber}.")
                        .FontSize(options.BaseFontSize)
                        .FontColor(Colors.Grey.Darken2);
                    row.RelativeItem().Text(text =>
                    {
                        RenderFormattedText(text, content, options);
                    });
                });
                
                itemNumber++;
            }
        });
    }

    private void RenderTable(ColumnDescriptor column, MarkdownElement element, PdfGenerationOptions options)
    {
        if (element.TableRows == null || element.TableRows.Count == 0) return;

        var hasHeader = element.Metadata == "header";
        var columnCount = element.TableRows.Max(r => r.Length);
        
        if (columnCount == 0) return;

        column.Item().PaddingVertical(8).Table(table =>
        {
            // Define columns with equal width
            table.ColumnsDefinition(columns =>
            {
                for (int i = 0; i < columnCount; i++)
                {
                    columns.RelativeColumn();
                }
            });

            var isFirstRow = true;
            foreach (var row in element.TableRows)
            {
                var isHeader = hasHeader && isFirstRow;
                
                for (int i = 0; i < columnCount; i++)
                {
                    var cellContent = i < row.Length ? row[i] : "";
                    
                    var cell = table.Cell()
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten1)
                        .Padding(5);

                    if (isHeader)
                    {
                        cell.Background(Colors.Blue.Lighten4)
                            .Text(text =>
                            {
                                var span = text.Span(CleanInlineFormatting(cellContent));
                                span.Bold().FontSize(options.BaseFontSize);
                            });
                    }
                    else
                    {
                        cell.Text(text =>
                        {
                            RenderFormattedText(text, cellContent, options);
                        });
                    }
                }

                isFirstRow = false;
            }
        });
    }

    private void RenderCodeBlock(ColumnDescriptor column, MarkdownElement element, PdfGenerationOptions options)
    {
        var language = element.Metadata ?? "";
        
        column.Item()
            .PaddingVertical(4)
            .Background(Colors.Grey.Lighten4)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(10)
            .Column(codeColumn =>
            {
                if (!string.IsNullOrWhiteSpace(language))
                {
                    codeColumn.Item()
                        .PaddingBottom(4)
                        .Text(language.ToUpperInvariant())
                        .FontSize(options.BaseFontSize - 3)
                        .FontColor(Colors.Grey.Darken1)
                        .Bold();
                }

                codeColumn.Item().Text(element.Content ?? "")
                    .FontSize(options.BaseFontSize - 1)
                    .FontColor(Colors.Grey.Darken3);
            });
    }

    private void RenderBlockquote(ColumnDescriptor column, MarkdownElement element, PdfGenerationOptions options)
    {
        column.Item()
            .PaddingVertical(4)
            .PaddingLeft(15)
            .BorderLeft(3)
            .BorderColor(Colors.Blue.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .Padding(10)
            .Text(text =>
            {
                text.Span(element.Content ?? "")
                    .FontSize(options.BaseFontSize)
                    .Italic()
                    .FontColor(Colors.Grey.Darken2);
            });
    }

    #region Helper Classes

    private class MarkdownElement
    {
        public ElementType Type { get; set; }
        public string? Content { get; set; }
        public int Level { get; set; } // For headers
        public string? Metadata { get; set; } // Language for code, "header" for tables
        public List<string[]>? TableRows { get; set; }
        public List<(int indent, string content)>? ListItems { get; set; }
    }

    private enum ElementType
    {
        BlankLine,
        Header,
        Paragraph,
        BulletList,
        NumberedList,
        Table,
        CodeBlock,
        Blockquote,
        HorizontalRule
    }

    private class TextSegment
    {
        public string Text { get; set; } = "";
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsCode { get; set; }
        public bool IsStrikethrough { get; set; }
    }

    #endregion
}
