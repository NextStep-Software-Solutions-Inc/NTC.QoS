namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Service for managing MCP (Model Context Protocol) server functionality.
/// Provides tools for AI to directly query database through API endpoints.
/// </summary>
public interface IMCPServerService
{
    /// <summary>
    /// Gets the list of available MCP tools that can be used by AI models
    /// </summary>
    Task<string> GetToolsAsync();

    /// <summary>
    /// Executes an MCP tool with the provided parameters
    /// </summary>
    /// <param name="toolName">Name of the tool to execute</param>
    /// <param name="parameters">JSON parameters for the tool</param>
    /// <returns>JSON result from the tool execution</returns>
    Task<string> ExecuteToolAsync(string toolName, string parameters);
}
