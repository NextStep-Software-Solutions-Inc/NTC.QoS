using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Service for loading and providing the global AI instruction/context.
/// 
/// <para><b>PURPOSE:</b></para>
/// <para>This service loads the "Global AI Instruction.md" file which contains the base 
/// system instruction for all AI requests. It defines:</para>
/// <list type="bullet">
///   <item>AI assistant role and scope for the CMTS &amp; Broadband Mapping Project</item>
///   <item>Allowed operations (explaining methodology, interpreting reports, etc.)</item>
///   <item>Limitations (no data invention, no policy decisions, etc.)</item>
///   <item>Data boundaries (only validated CMTS datasets, official reports)</item>
///   <item>Response style rules (formal, neutral, factual)</item>
///   <item>Project knowledge base (objectives, methodology, classification logic)</item>
/// </list>
/// 
/// <para><b>LOADING BEHAVIOR:</b></para>
/// <para>The instruction file is loaded once at application startup (singleton service).
/// The service searches for the file in multiple locations:</para>
/// <list type="number">
///   <item>AppContext.BaseDirectory (deployment directory)</item>
///   <item>Current working directory</item>
///   <item>Relative path from current location</item>
/// </list>
/// <para>If the file is not found, a default minimal instruction is used.</para>
/// 
/// <para><b>USAGE IN RAG FLOW:</b></para>
/// <para>The global instruction is prepended to every AI prompt as "SYSTEM INSTRUCTION",
/// ensuring the AI operates within the defined scope and limitations for all requests.</para>
/// </summary>
public class AIInstructionService : IAIInstructionService
{
    private readonly ILogger<AIInstructionService> _logger;
    private readonly string _globalInstruction;

    public AIInstructionService(ILogger<AIInstructionService> logger)
    {
        _logger = logger;
        _globalInstruction = LoadGlobalInstruction();
    }

    /// <summary>
    /// Gets the global AI system instruction/context
    /// </summary>
    public string GetGlobalInstruction() => _globalInstruction;

    private string LoadGlobalInstruction()
    {
        try
        {
            // Try to load from file system (relative to the API root)
            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Global AI Instruction.md"),
                Path.Combine(Directory.GetCurrentDirectory(), "Global AI Instruction.md"),
                "Global AI Instruction.md"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    var content = File.ReadAllText(path);
                    _logger.LogInformation("Loaded global AI instruction from: {Path}", path);
                    return content;
                }
            }

            _logger.LogWarning("Global AI Instruction file not found. Using default instruction.");
            return GetDefaultInstruction();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load global AI instruction file");
            return GetDefaultInstruction();
        }
    }

    private static string GetDefaultInstruction()
    {
        return @"# AI CHAT ASSISTANT – CMTS & Broadband Mapping Project

You are an AI Chat Assistant designed to support the CMTS and Broadband Mapping Project of the National Telecommunications Commission (NTC).

Your role is to provide accurate, neutral, and technically grounded information about:
- The project objectives
- Methodology
- Data sources
- Reporting outputs
- Service classification logic

You must respond only within the scope of the project and based on official, validated data and approved documentation.

Use formal, neutral, government-appropriate language. Be factual, concise, and technically accurate.";
    }
}
