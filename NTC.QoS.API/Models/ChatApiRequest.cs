using Microsoft.AspNetCore.Http;

namespace NTC.QoS.API.Models;

/// <summary>
/// API-level request for AI chat assistant (uses ASP.NET Core types)
/// </summary>
public class ChatApiRequest
{
    /// <summary>
    /// User's prompt or question
    /// </summary>
    public required string Prompt { get; set; }

    /// <summary>
    /// Optional file attachments (images, PDFs, Excel, text files)
    /// </summary>
    public IFormFile[]? Files { get; set; }
}
