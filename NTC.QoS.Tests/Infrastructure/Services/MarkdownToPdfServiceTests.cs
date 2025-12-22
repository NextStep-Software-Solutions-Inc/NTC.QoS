using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Services;
using Xunit;

namespace NTC.QoS.Tests.Infrastructure.Services;

public class MarkdownToPdfServiceTests
{
    private readonly Mock<ILogger<MarkdownToPdfService>> _loggerMock;
    private readonly MarkdownToPdfService _service;

    public MarkdownToPdfServiceTests()
    {
        _loggerMock = new Mock<ILogger<MarkdownToPdfService>>();
        _service = new MarkdownToPdfService(_loggerMock.Object);
    }

    [Fact]
    public void ConvertToPdf_WithSimpleMarkdown_ReturnsValidPdf()
    {
        // Arrange
        var markdown = @"# Test Report

This is a simple test report.

## Section 1

Some content here.

- Bullet point 1
- Bullet point 2
- Bullet point 3";

        // Act
        var result = _service.ConvertToPdf(markdown, "Test Report");

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
        // PDF files start with %PDF
        result[0].Should().Be((byte)'%');
        result[1].Should().Be((byte)'P');
        result[2].Should().Be((byte)'D');
        result[3].Should().Be((byte)'F');
    }

    [Fact]
    public void ConvertToPdf_WithTableMarkdown_ReturnsValidPdf()
    {
        // Arrange - This is the critical test case for the table rendering fix
        var markdown = @"# Coverage Report

## Summary Table

| Region | Population | Coverage % | Status |
|--------|------------|------------|--------|
| North | 150,000 | 85% | Served |
| South | 200,000 | 72% | Underserved |
| East | 180,000 | 90% | Served |
| West | 120,000 | 45% | Unserved |

## Conclusion

The table above shows the coverage distribution across regions.";

        // Act
        var result = _service.ConvertToPdf(markdown, "Coverage Report");

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
        // PDF should be larger when it contains a table
        result.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public void ConvertToPdf_WithCodeBlock_ReturnsValidPdf()
    {
        // Arrange
        var markdown = @"# Code Example

Here is some code:

```python
def hello_world():
    print('Hello, World!')

hello_world()
```

End of example.";

        // Act
        var result = _service.ConvertToPdf(markdown, "Code Example");

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ConvertToPdf_WithBlockquote_ReturnsValidPdf()
    {
        // Arrange
        var markdown = @"# Important Notice

> This is a blockquote.
> It can span multiple lines.

Regular text after the quote.";

        // Act
        var result = _service.ConvertToPdf(markdown, "Notice");

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ConvertToPdf_WithAllHeaders_ReturnsValidPdf()
    {
        // Arrange
        var markdown = @"# Heading 1

## Heading 2

### Heading 3

#### Heading 4

##### Heading 5

###### Heading 6

Regular paragraph text.";

        // Act
        var result = _service.ConvertToPdf(markdown, "Headers Test");

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ConvertToPdf_WithInlineFormatting_ReturnsValidPdf()
    {
        // Arrange
        var markdown = @"# Formatting Test

This text has **bold text** and *italic text* and `inline code`.

We also support ~~strikethrough text~~ and [links](http://example.com).";

        // Act
        var result = _service.ConvertToPdf(markdown, "Formatting Test");

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ConvertToPdf_WithNumberedList_ReturnsValidPdf()
    {
        // Arrange
        var markdown = @"# Steps to Follow

1. First step
2. Second step
3. Third step
4. Fourth step";

        // Act
        var result = _service.ConvertToPdf(markdown, "Steps");

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ConvertToPdf_WithHorizontalRule_ReturnsValidPdf()
    {
        // Arrange
        var markdown = @"# Section 1

Content here.

---

# Section 2

More content.";

        // Act
        var result = _service.ConvertToPdf(markdown, "Sections");

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ConvertToPdf_WithEmptyContent_ReturnsValidPdf()
    {
        // Arrange
        var markdown = "";

        // Act
        var result = _service.ConvertToPdf(markdown, "Empty Report");

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ConvertToPdf_WithNullContent_ReturnsValidPdf()
    {
        // Arrange
        string? markdown = null;

        // Act
        var result = _service.ConvertToPdf(markdown!, "Null Report");

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ConvertToPdf_WithLandscapeOrientation_ReturnsValidPdf()
    {
        // Arrange
        var markdown = @"# Wide Report

| Col1 | Col2 | Col3 | Col4 | Col5 | Col6 | Col7 |
|------|------|------|------|------|------|------|
| A | B | C | D | E | F | G |";

        var options = new PdfGenerationOptions
        {
            Orientation = PageOrientation.Landscape
        };

        // Act
        var result = _service.ConvertToPdf(markdown, "Wide Report", options);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ConvertToPdf_WithCustomOptions_ReturnsValidPdf()
    {
        // Arrange
        var markdown = "# Custom Report\n\nSome content.";
        var options = new PdfGenerationOptions
        {
            OrganizationName = "Test Organization",
            IncludePageNumbers = true,
            IncludeTimestamp = true,
            BaseFontSize = 12,
            MarginCm = 2.5f
        };

        // Act
        var result = _service.ConvertToPdf(markdown, "Custom Report", options);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ConvertToPdfAsync_WithMarkdown_ReturnsValidPdf()
    {
        // Arrange
        var markdown = "# Async Test\n\nAsync content.";

        // Act
        var result = await _service.ConvertToPdfAsync(markdown, "Async Test");

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ConvertToPdf_WithComplexTable_ReturnsValidPdf()
    {
        // Arrange - Test complex real-world table scenario
        var markdown = @"# Network Coverage Analysis Report

## Executive Summary

This report provides a comprehensive analysis of network coverage across the region.

## Coverage by Municipality

| Municipality | Total Barangays | Served | Underserved | Unserved | Coverage % |
|-------------|-----------------|--------|-------------|----------|------------|
| Municipality A | 45 | 38 | 5 | 2 | 84.4% |
| Municipality B | 32 | 28 | 3 | 1 | 87.5% |
| Municipality C | 28 | 15 | 8 | 5 | 53.6% |
| Municipality D | 51 | 42 | 6 | 3 | 82.4% |
| Municipality E | 39 | 35 | 3 | 1 | 89.7% |
| **Total** | **195** | **158** | **25** | **12** | **81.0%** |

## Key Findings

1. **Overall Coverage**: 81% of barangays have adequate network coverage
2. **Priority Areas**: Municipality C requires immediate attention with only 53.6% coverage
3. **Best Performing**: Municipality E leads with 89.7% coverage

## Recommendations

- Prioritize infrastructure deployment in Municipality C
- Conduct detailed assessments of unserved barangays
- Implement targeted programs for underserved areas

---

*Report generated on: December 2024*";

        // Act
        var result = _service.ConvertToPdf(markdown, "Network Coverage Analysis Report");

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
        // Complex reports should generate larger PDFs
        result.Length.Should().BeGreaterThan(2000);
    }

    [Fact]
    public void ConvertToPdf_WithMultipleTables_ReturnsValidPdf()
    {
        // Arrange
        var markdown = @"# Multi-Table Report

## Table 1: Overview

| Metric | Value |
|--------|-------|
| Total | 100 |
| Average | 50 |

## Table 2: Details

| Item | Category | Status |
|------|----------|--------|
| A | Cat1 | Active |
| B | Cat2 | Inactive |
| C | Cat1 | Active |";

        // Act
        var result = _service.ConvertToPdf(markdown, "Multi-Table Report");

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }
}
