using System;
using System.Linq;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Helpers;

/// <summary>
/// Helper utilities for formatting and sanitizing blob storage paths, container names, and region codes.
/// </summary>
public static class BlobStoragePathHelper
{
    public const string DefaultRegionPrefix = "region-default";
    public const string ProductionContainer = "production";
    public const string DevelopmentContainer = "development";

    /// <summary>
    /// Sanitizes a raw region code into a valid Azure Blob Storage path segment
    /// (lowercase alphanumeric and hyphens, prefixed with 'region-').
    /// </summary>
    public static string SanitizeRegionCode(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return DefaultRegionPrefix;
        }

        var cleanCode = new string(rawCode.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray()).ToLowerInvariant();
        if (string.IsNullOrEmpty(cleanCode))
        {
            return DefaultRegionPrefix;
        }

        return cleanCode.StartsWith("region-", StringComparison.OrdinalIgnoreCase) ? cleanCode : $"region-{cleanCode}";
    }

    /// <summary>
    /// Creates the blob path with region-based folder structure.
    /// Format: region-XX/file-type/filename
    /// </summary>
    public static string GetBlobPathWithRegion(FileTypeEnum fileType, string regionCode, string fileName)
    {
        var fileTypePrefix = fileType switch
        {
            FileTypeEnum.Cell => "cells",
            FileTypeEnum.NAP => "naps",
            FileTypeEnum.Speedtest => "speedtests",
            FileTypeEnum.Cellular => "cellulars",
            FileTypeEnum.CellularCoverage => "cellular-coverages",
            FileTypeEnum.WirelessValidation => "wireless-validations",
            _ => "uploads"
        };

        return $"{regionCode}/{fileTypePrefix}/{fileName}";
    }

    /// <summary>
    /// Gets the appropriate container name based on the ASPNETCORE_ENVIRONMENT value.
    /// </summary>
    public static string GetContainerName(string? environment)
    {
        return environment?.ToLowerInvariant() switch
        {
            "production" => ProductionContainer,
            "staging" => ProductionContainer,
            _ => DevelopmentContainer
        };
    }
}
