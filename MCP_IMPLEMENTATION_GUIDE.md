# MCP Server Implementation Guide

## Overview

The NTC QoS system now includes a Model Context Protocol (MCP) server that allows AI models to directly query the database through structured tool calls. This enables the AI to generate more accurate and up-to-date reports by accessing live data instead of relying solely on file attachments.

## What is MCP?

Model Context Protocol (MCP) is a protocol that enables AI models to interact with external tools and data sources in a structured way. In this implementation:

1. The AI receives tool definitions in its system prompt
2. When the AI needs data, it requests a tool call using a JSON structure
3. The MCP server executes the tool and returns the results
4. The AI incorporates the results into its response

## Available Tools

The MCP server currently provides 5 tools for querying the NTC QoS database:

### 1. get_providers
Query telecommunications service providers with pagination and filtering.

**Parameters:**
- `pageSize` (integer, optional): Number of results per page (default: 10)
- `pageNumber` (integer, optional): Page number (default: 1)
- `serviceIds` (array of GUIDs, optional): Filter by service IDs
- `names` (array of strings, optional): Filter by provider names

**Example:**
```json
{
  "tool": "get_providers",
  "parameters": {
    "pageSize": 20,
    "pageNumber": 1,
    "names": ["PLDT", "Globe"]
  }
}
```

### 2. get_connectivity_analysis
Get detailed connectivity analysis based on CMTS Broadband Mapping Rubric.

**Parameters:**
- `locationIds` (array of GUIDs, required): Location IDs to analyze
- `locationType` (string, required): "Barangay", "Municipality", "Province", or "Region"
- `filterByStatus` (string, optional): "Served", "Underserved", or "Unserved"
- `includeSpeedTestData` (boolean, optional): Include speed test data (default: false)
- `includeInfrastructureData` (boolean, optional): Include infrastructure details (default: false)

**Example:**
```json
{
  "tool": "get_connectivity_analysis",
  "parameters": {
    "locationIds": ["550e8400-e29b-41d4-a716-446655440000"],
    "locationType": "Municipality",
    "includeSpeedTestData": true
  }
}
```

### 3. get_area_broadband_info
Get broadband infrastructure information for a geographic bounding box.

**Parameters:**
- `minLatitude` (number, required): Minimum latitude (-5 to 25)
- `minLongitude` (number, required): Minimum longitude (110 to 135)
- `maxLatitude` (number, required): Maximum latitude (-5 to 25)
- `maxLongitude` (number, required): Maximum longitude (110 to 135)

**Example:**
```json
{
  "tool": "get_area_broadband_info",
  "parameters": {
    "minLatitude": 14.0,
    "minLongitude": 120.0,
    "maxLatitude": 15.0,
    "maxLongitude": 121.0
  }
}
```

### 4. get_point_broadband_info
Get broadband infrastructure within a radius from a center point.

**Parameters:**
- `latitude` (number, required): Center point latitude (-5 to 25)
- `longitude` (number, required): Center point longitude (110 to 135)
- `radiusMeters` (number, required): Radius in meters (max 50000)

**Example:**
```json
{
  "tool": "get_point_broadband_info",
  "parameters": {
    "latitude": 14.5,
    "longitude": 121.0,
    "radiusMeters": 5000
  }
}
```

### 5. generate_connectivity_assessment
Generate or update connectivity assessments for specific locations.

**Parameters:**
- `locationIds` (array of GUIDs, required): Location IDs to assess
- `locationType` (string, required): "Barangay", "Municipality", "Province", or "Region"
- `forceRecalculate` (boolean, optional): Force recalculation (default: false)

**Example:**
```json
{
  "tool": "generate_connectivity_assessment",
  "parameters": {
    "locationIds": ["550e8400-e29b-41d4-a716-446655440000"],
    "locationType": "Barangay",
    "forceRecalculate": true
  }
}
```

## How AI Uses MCP Tools

When an AI model (like the report generator or chat assistant) needs database information, it can request a tool call by including a JSON code block in its response:

```markdown
```mcp-tool-call
{
  "tool": "get_providers",
  "parameters": {
    "pageSize": 10,
    "pageNumber": 1
  }
}
\```
```

The system:
1. Detects the tool call request
2. Executes the tool via the MCP server
3. Returns the JSON results to the AI
4. The AI incorporates the data into its response

## Implementation Details

### Architecture

```
User Request
    ↓
GenAI Handler (Chat/Report)
    ↓
Enhanced Prompt (includes MCP tool definitions)
    ↓
AI Model (Google Gemini)
    ↓
[Optional] Tool Call Request
    ↓
MCPServerService.ExecuteToolAsync()
    ↓
MediatR → Existing API Handlers
    ↓
Database Query
    ↓
JSON Response → AI Model
    ↓
Final Response to User
```

### Key Files

- **Service**: `NTC.QoS.Application/Services/MCP/MCPServerService.cs`
- **Interface**: `NTC.QoS.Domain/Interfaces/IMCPServerService.cs`
- **Integration**: 
  - `NTC.QoS.Application/Features/GenAI/Handlers/Commands/GenerateReportHandler.cs`
  - `NTC.QoS.Application/Features/GenAI/Handlers/Commands/ChatHandler.cs`
- **Tests**: `NTC.QoS.Tests/Application/Services/MCP/MCPServerServiceTests.cs`

### Dependencies

The MCP implementation uses:
- **ModelContextProtocol** v0.5.0-preview.1 (Microsoft official package)
- **MediatR** for executing API requests
- **System.Text.Json** for JSON serialization

## Testing

### Unit Tests

Run the MCP server tests:
```bash
dotnet test --filter "FullyQualifiedName~MCP"
```

### Integration Testing

The MCP tools are automatically available to the AI in:
1. `/genai/chat` endpoint - Chat with AI assistant
2. `/genai/generate-report` endpoint - Generate reports

To test:
1. Send a chat or report request
2. Ask the AI to query database information
3. The AI should use MCP tools to fetch data

Example prompts:
- "What telecommunications providers are available?"
- "Analyze the connectivity status for Region 10"
- "Show me broadband infrastructure near Manila"

## Future Enhancements

Potential improvements:
1. Add more tools for other API endpoints (users, networks, studies)
2. Implement tool result caching
3. Add support for batch tool calls
4. Create a tool usage analytics dashboard
5. Add rate limiting for tool calls
6. Implement tool call authorization

## Security Considerations

- All tool calls go through existing API authorization
- User permissions are checked via MediatR handlers
- No direct database access - all queries use existing validated endpoints
- Tool parameters are validated before execution
- Errors are logged but not exposed to users

## Troubleshooting

### Tool Not Found
If AI reports a tool is not available, verify:
1. MCPServerService is registered in DI (`ApplicationDependencyInjection.cs`)
2. Tool name matches exactly (case-sensitive)
3. Check logs for any initialization errors

### Tool Execution Fails
If tool execution fails:
1. Check that parameters match the expected schema
2. Verify user has required permissions
3. Review application logs for detailed error messages
4. Ensure database is accessible and data exists

### AI Not Using Tools
If AI doesn't use tools:
1. Verify tool definitions are included in the prompt
2. Check that the AI model supports function calling
3. Try being more explicit in your request (e.g., "Query the database for...")
