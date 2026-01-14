using Microsoft.AspNetCore.Http;

namespace NTC.QoS.API.Models;

/// <summary>
/// API-level request for AI report generation (uses ASP.NET Core types)
/// </summary>
public class GenerateReportApiRequest
{
    /// <summary>
    /// Instructions for the report generation
    /// </summary>
    public required string Prompt { get; set; }

    /// <summary>
    /// Resource files to analyze for the report
    /// </summary>
    public IFormFile[]? Files { get; set; }
}
