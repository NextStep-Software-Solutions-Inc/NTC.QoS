# Google Gen AI Integration - Implementation Summary

## Overview
This document describes the implementation of Google Generative AI integration for the NTC QoS platform, providing AI-powered chat assistant and report generation features using the **official Google.GenAI SDK** with **real-time streaming responses** and **multimodal file processing**.

## Implementation Details

### 1. SDK and Model
- **Package**: `Google.GenAI` v0.9.0 (**Official Google Generative AI SDK for .NET**)
- **Model**: Gemini 2.0 Flash Experimental (Latest experimental model with multimodal support)
- **No vulnerabilities** found in dependency check
- **Repository**: https://github.com/googleapis/dotnet-genai
- **Streaming Support**: Real-time response streaming via Server-Sent Events (SSE)
- **Multimodal Support**: Images, PDFs, and text files are processed by the AI model

### 2. Architecture

#### Domain Layer (`NTC.QoS.Domain`)
- **Interface**: `IGenAIService.cs` - Service contract for GenAI operations
- **DTOs**:
  - `ChatRequestDto.cs` - Request model for chat endpoint
  - `ChatResponseDto.cs` - Response model for chat endpoint
  - `GenerateReportRequestDto.cs` - Request model for report generation
  - `GenerateReportResponseDto.cs` - Response model for report generation
- **Permissions**:
  - `Permissions.ChatWithAI` = "genai.chat"
  - `Permissions.GenerateReports` = "genai.generate_reports"

#### Infrastructure Layer (`NTC.QoS.Infrastructure`)
- **Service**: `GenAIService.cs` - Implementation of IGenAIService
  - Uses Gemini 2.0 Flash Experimental model
  - **Streaming responses**: Real-time AI response chunks via `IAsyncEnumerable<string>`
  - **Multimodal file processing**: 
    - Images (JPEG, PNG, WebP, HEIC, GIF) processed via InlineData/Blob
    - PDF files processed via InlineData/Blob
    - Text files (.txt, .csv, .json, .xml) content extracted and included
  - **File validation**: Maximum 10 files per request
  - Comprehensive error handling and logging
- **Registration**: Registered in `ExternalServiceExtensions.cs`

#### Application Layer (`NTC.QoS.Application`)
- **Commands**:
  - `ChatRequest.cs` - MediatR command for chat (streaming)
  - `GenerateReportRequest.cs` - MediatR command for report generation (streaming)
- **Handlers**:
  - `ChatHandler.cs` - Processes chat requests and streams responses
  - `GenerateReportHandler.cs` - Processes report generation requests and streams responses
- **Validators**:
  - `ChatRequestValidator.cs` - Validates chat requests (max 10,000 chars, max 10 files)
  - `GenerateReportRequestValidator.cs` - Validates report requests (max 10,000 chars, max 10 files)

#### API Layer (`NTC.QoS.API`)
- **Controller**: `GenAIController.cs`
  - `POST /genai/chat` - Chat with AI assistant (streaming via SSE)
  - `POST /genai/generate-report` - Generate AI-powered reports (streaming via SSE)
  - Comprehensive XML documentation with Swagger annotations
  - Permission-based authorization
  - Multipart/form-data support with `[Consumes]` attribute
  - Real-time streaming responses via Server-Sent Events

### 3. Configuration

The API key must be configured via user secrets or environment variables:

```json
{
  "GoogleAI": {
    "ApiKey": "YOUR_GOOGLE_AI_API_KEY_HERE"
  }
}
```

#### Development (User Secrets)
```bash
cd NTC.QoS.API
dotnet user-secrets set "GoogleAI:ApiKey" "YOUR_API_KEY"
```

#### Production (Environment Variables)
```bash
export GoogleAI__ApiKey="YOUR_API_KEY"
```

Or in Kubernetes:
```yaml
env:
  - name: GoogleAI__ApiKey
    valueFrom:
      secretKeyRef:
        name: genai-secrets
        key: api-key
```

### 4. Authorization and Permissions

New permissions have been added and seeded for appropriate roles:

- **`genai.chat`** - Allows access to the AI chat assistant
- **`genai.generate_reports`** - Allows generating AI-powered reports

These permissions are automatically assigned to:
- Super Admin (all permissions)
- Admin (all permissions except system administration)

### 5. API Endpoints

#### Chat with AI Assistant
**Endpoint**: `POST /genai/chat`

**Request**:
```http
POST /genai/chat HTTP/1.1
Content-Type: multipart/form-data
Authorization: Bearer {token}

prompt: What are the coverage statistics for Region 10?
files: [optional, max 10 file attachments]
```

**Supported File Types**:
- Images: JPEG, PNG, WebP, HEIC, GIF (processed by AI vision)
- Documents: PDF files (content analyzed by AI)
- Text files: .txt, .csv, .json, .xml (content extracted and included)

**Response** (Server-Sent Events):
```
Content-Type: text/event-stream

data: Based on the available data
data: , the coverage statistics show...
data:  excellent performance in Region 10.
```

**Key for attachments**: `files` (not `attachments`)

#### Generate Report
**Endpoint**: `POST /genai/generate-report`

**Request**:
```http
POST /genai/generate-report HTTP/1.1
Content-Type: multipart/form-data
Authorization: Bearer {token}

prompt: Generate a network performance analysis report
files: [optional, max 10 resource files]
```

**Supported File Types**:
- Images: JPEG, PNG, WebP, HEIC, GIF (for visual data analysis)
- Documents: PDF files (content analyzed)
- Text files: .txt, .csv, .json, .xml (content included in analysis)
- Spreadsheets: Excel files (.xlsx, .xls) - Note: extracted as text

**Response** (Server-Sent Events):
```
Content-Type: text/event-stream

data: # Executive Summary
data: 

data: Based on the provided network data...
```

**Key for attachments**: `files` (not `attachments`)

### 6. Security Considerations

1. **API Key Protection**:
   - API key is loaded from configuration (not hardcoded)
   - Not stored in field to minimize memory exposure
   - Should be stored in Azure Key Vault or GitHub Secrets for production

2. **Authorization**:
   - All endpoints require authentication (`[Authorize]`)
   - Permission-based access control using `[RequirePermission]`
   - Follows existing authorization patterns

3. **Input Validation**:
   - FluentValidation validators enforce constraints
   - Maximum prompt length: 10,000 characters
   - **Maximum file attachments: 10 files per request**
   - File types validated and processed appropriately

4. **Error Handling**:
   - Comprehensive try-catch blocks
   - Detailed logging for troubleshooting
   - User-friendly error messages

### 7. Current Features and Capabilities

#### Implemented:
- ✅ Text-based prompts fully supported
- ✅ **Streaming responses via Server-Sent Events (SSE)**
- ✅ Chat assistant functionality with real-time streaming
- ✅ Report generation with structured output and streaming
- ✅ **Multimodal file processing**:
  - ✅ Images (JPEG, PNG, WebP, HEIC, GIF) - analyzed by AI vision
  - ✅ PDF files - content analyzed by AI
  - ✅ Text files (.txt, .csv, .json, .xml) - content extracted and included
- ✅ **File attachment validation**: Max 10 files per request
- ✅ Comprehensive Swagger/OpenAPI documentation

#### Current Limitations:
- ⚠️ Excel files (.xlsx, .xls) are accepted but text extraction is not yet implemented
- ℹ️ Large files may take longer to process

### 8. Future Enhancements

1. **Excel File Processing**:
   - Implement text extraction from Excel files (.xlsx, .xls)
   - Parse spreadsheet data for better analysis

2. **Conversation History**:
   - Maintain chat context across multiple requests
   - Session management for continuity

3. **Advanced Features**:
   - Custom prompt templates
   - Fine-tuned models for domain-specific tasks
   - Rate limiting and quota management
   - Response caching for common queries

### 9. Testing

To test the endpoints:

1. **Set up the API key**:
   ```bash
   dotnet user-secrets set "GoogleAI:ApiKey" "YOUR_KEY" --project NTC.QoS.API
   ```

2. **Run the application**:
   ```bash
   dotnet run --project NTC.QoS.API
   ```

3. **Test chat endpoint** (streaming response):
   ```bash
   curl -X POST "https://localhost:7000/genai/chat" \
     -H "Authorization: Bearer YOUR_TOKEN" \
     -F "prompt=What is the NTC QoS platform?" \
     -N  # Enable streaming
   ```

4. **Test with file attachment**:
   ```bash
   curl -X POST "https://localhost:7000/genai/chat" \
     -H "Authorization: Bearer YOUR_TOKEN" \
     -F "prompt=Analyze this image" \
     -F "files=@/path/to/image.jpg" \
     -N  # Enable streaming
   ```

5. **Test report generation** (streaming response):
   ```bash
   curl -X POST "https://localhost:7000/genai/generate-report" \
     -H "Authorization: Bearer YOUR_TOKEN" \
     -F "prompt=Generate a summary report of network coverage" \
     -F "files=@/path/to/data.csv" \
     -N  # Enable streaming
   ```

**Note**: The `-N` flag is important for streaming responses. Without it, curl will buffer the entire response.

### 10. Best Practices

1. **Cost Management**:
   - Gemini 2.0 Flash Experimental is currently free tier but has rate limits
   - Monitor usage and implement caching if needed
   - Consider upgrading to paid tier for production use

2. **Prompt Engineering**:
   - Provide clear, specific instructions
   - Include context about the data being analyzed
   - Structure prompts for better output quality
   - Reference attached files in your prompt

3. **File Attachments**:
   - Use supported file types for best results
   - Keep file sizes reasonable (large files take longer to process)
   - Maximum 10 files per request
   - Describe what you want analyzed in the prompt

4. **Streaming Responses**:
   - Use appropriate HTTP clients that support streaming
   - Handle connection interruptions gracefully
   - Display chunks incrementally for better UX

5. **Error Handling**:
   - Always check for null responses
   - Handle rate limit errors gracefully
   - Provide meaningful feedback to users
   - Log errors for troubleshooting

## Compliance with Requirements

✅ **SDK**: Uses latest official Google Generative AI SDK for .NET  
✅ **Model**: Uses Gemini 2.0 Flash Experimental (latest model)  
✅ **Capabilities**: AI assistant and report generation  
✅ **Endpoints**: Chat and report generation endpoints implemented  
✅ **File Support**: Multipart/form-data support with actual file processing  
✅ **Supported Files**: Images (JPEG, PNG, WebP, HEIC, GIF), PDFs, text files (.txt, .csv, .json, .xml)  
✅ **File Validation**: Maximum 10 files per request  
✅ **Streaming**: Real-time response streaming via Server-Sent Events (SSE)  
✅ **Permissions**: New permissions defined and seeded  
✅ **Security**: Follows existing permission patterns  
✅ **Documentation**: Comprehensive XML comments and Swagger annotations  
✅ **Best Practices**: SOLID and DRY principles followed  

## Support and Troubleshooting

### Common Issues:

1. **"GoogleAI:ApiKey is not configured"**:
   - Ensure the API key is set in user secrets or environment variables
   - Check the configuration key format: `GoogleAI:ApiKey` (colon) or `GoogleAI__ApiKey` (double underscore for env vars)

2. **403 Forbidden**:
   - User lacks required permissions (`genai.chat` or `genai.generate_reports`)
   - Check role assignments in the database

3. **"Cannot upload more than 10 files"**:
   - Reduce the number of file attachments to 10 or fewer
   - This limit is enforced via FluentValidation

4. **Streaming not working**:
   - Ensure client supports Server-Sent Events (SSE)
   - Use the `-N` flag with curl for streaming
   - Check that proper Content-Type header is set (text/event-stream)

5. **File not processed**:
   - Verify file type is supported
   - Check file size (very large files may timeout)
   - Review logs for processing errors

## Conclusion

The Google Gen AI integration has been successfully implemented with full streaming support and multimodal file processing capabilities. The implementation follows best practices and the existing codebase patterns. The system is production-ready for text-based interactions with real-time streaming and supports image, PDF, and text file analysis.
