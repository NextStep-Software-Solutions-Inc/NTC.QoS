# GenAI RAG System - Complete Flow Documentation

## Overview

This document describes the complete flow of the RAG (Retrieval Augmented Generation) system
implemented for the CMTS & Broadband Mapping Project AI Assistant.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                                    CLIENT REQUEST                                     │
│                    POST /genai/chat or POST /genai/generate-report                   │
│                         (multipart/form-data: prompt + files)                         │
└─────────────────────────────────────────────────────────────────────────────────────┘
                                           │
                                           ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                                  API CONTROLLER                                       │
│                              GenAIController.cs                                       │
│  • Receives HTTP request                                                              │
│  • Maps API models to domain DTOs                                                     │
│  • Sets up SSE (Server-Sent Events) headers                                          │
│  • Dispatches to MediatR handler                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
                                           │
                                           ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                            APPLICATION LAYER HANDLER                                  │
│                    ChatHandler.cs / GenerateReportHandler.cs                         │
│                                                                                       │
│  STEP 1: Load Global AI Instruction                                                   │
│  ├── IAIInstructionService.GetGlobalInstruction()                                    │
│  └── Loads "Global AI Instruction.md" from API root                                  │
│                                                                                       │
│  STEP 2: User Session Management (ChatHandler only)                                   │
│  ├── Get authenticated user from IUserContextService                                 │
│  ├── Retrieve chat history from IMemoryCache (keyed by userId)                       │
│  └── Session expires after 30 minutes of inactivity                                  │
│                                                                                       │
│  STEP 3: RAG Context Retrieval                                                        │
│  ├── Generate embedding for user's prompt via IGenAIService.GetEmbeddingAsync()     │
│  ├── Search for similar reports via IGeneratedReportRepository.FindSimilarReportsAsync()│
│  └── Returns top 3 most semantically similar report contents                         │
│                                                                                       │
│  STEP 4: Build Enhanced Prompt                                                        │
│  ├── SYSTEM INSTRUCTION: Global AI Instruction (scope, limitations, project context) │
│  ├── CONTEXT FROM PREVIOUS REPORTS: RAG-retrieved report contents                    │
│  ├── CHAT HISTORY: Previous conversation messages (ChatHandler only)                 │
│  └── USER: Current user prompt                                                        │
│                                                                                       │
│  STEP 5: Stream AI Response                                                           │
│  ├── Call IGenAIService.ChatStreamAsync() / GenerateReportStreamAsync()             │
│  ├── Each chunk is JSON-serialized and written as SSE                                │
│  └── Response is flushed immediately for real-time streaming                         │
│                                                                                       │
│  STEP 6: Post-Processing                                                              │
│  ├── ChatHandler: Update session history in cache                                    │
│  └── GenerateReportHandler: Save report with embedding for future RAG retrieval     │
└─────────────────────────────────────────────────────────────────────────────────────┘
                                           │
                                           ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                            INFRASTRUCTURE LAYER                                       │
│                                                                                       │
│  GenAIService.cs (IGenAIService)                                                     │
│  ├── Uses Google.GenAI SDK with Gemini models                                        │
│  ├── ChatModel: gemini-2.0-flash-exp (content generation)                            │
│  ├── EmbeddingModel: text-embedding-004 (768-dimensional vectors)                    │
│  ├── API Key from GoogleAI:ApiKey config (env var: GoogleAI__ApiKey)                │
│  └── Streams responses via IAsyncEnumerable<string>                                  │
│                                                                                       │
│  AIInstructionService.cs (IAIInstructionService)                                     │
│  ├── Loads "Global AI Instruction.md" at startup                                     │
│  ├── Registered as Singleton (loaded once)                                           │
│  └── Provides project scope, limitations, and knowledge base                         │
│                                                                                       │
│  GeneratedReportRepository.cs (IGeneratedReportRepository)                           │
│  ├── FindSimilarReportsAsync(): Vector similarity search using L2 distance           │
│  ├── AddReportWithEmbeddingAsync(): Saves report with Pgvector embedding             │
│  └── Uses Pgvector.EntityFrameworkCore for PostgreSQL vector operations              │
└─────────────────────────────────────────────────────────────────────────────────────┘
                                           │
                                           ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                                  DATABASE                                             │
│                          PostgreSQL with pgvector extension                           │
│                                                                                       │
│  GeneratedReports Table                                                               │
│  ├── Id (GUID)                                                                        │
│  ├── Prompt (text)                                                                    │
│  ├── Content (text)                                                                   │
│  ├── Title (text)                                                                     │
│  ├── UserId (GUID)                                                                    │
│  ├── GeneratedAt (timestamp)                                                          │
│  ├── FilesAnalyzed (int)                                                              │
│  ├── FileMetadata (JSON)                                                              │
│  └── Embedding (vector(768)) - HNSW indexed for fast similarity search               │
└─────────────────────────────────────────────────────────────────────────────────────┘
                                           │
                                           ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                              STREAMING RESPONSE                                       │
│                          Server-Sent Events (SSE)                                     │
│                                                                                       │
│  Content-Type: text/event-stream                                                      │
│  Format: data: "chunk content"\n\n                                                   │
│  Each chunk is JSON-serialized to preserve structure                                  │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

## Key Components

### 1. Global AI Instruction (`Global AI Instruction.md`)
- Located in API project root
- Defines AI assistant scope, limitations, and project context
- Loaded once at application startup by `AIInstructionService`
- Included as "SYSTEM INSTRUCTION" in every AI request

### 2. RAG (Retrieval Augmented Generation)
- User prompts are converted to 768-dimensional embeddings
- PostgreSQL pgvector extension stores and searches embeddings
- Top 3 most similar previous reports are retrieved
- Context is prepended to user prompt for informed responses

### 3. Session Management (Chat only)
- User sessions stored in `IMemoryCache`
- Keyed by authenticated user ID
- Last 10 messages included in prompt
- 30-minute sliding expiration

### 4. Streaming Response
- Google GenAI SDK provides `IAsyncEnumerable<GenerateContentResponse>`
- Each chunk is immediately piped to client
- SSE format for real-time streaming
- JSON serialization preserves content structure

### 5. Report Persistence
- Generated reports saved to database
- Embedding generated and stored for future RAG retrieval
- Title extracted from markdown heading or truncated prompt
- File metadata preserved as JSON

## Configuration

### Environment Variables
- `GoogleAI__ApiKey`: Google AI Studio API key (injected via Docker environment variables / CI/CD secrets)

### appsettings.json
```json
{
  "GeminiSettings": {
    "ChatModel": "gemini-2.0-flash-exp",
    "EmbeddingModel": "text-embedding-004"
  }
}
```

## Clean Architecture

The implementation follows Clean Architecture principles:

- **Domain Layer**: Interfaces only (`IGenAIService`, `IAIInstructionService`, `IGeneratedReportRepository`)
- **Application Layer**: Handlers, business logic (no EF Core or Pgvector references)
- **Infrastructure Layer**: Implementations, database access, external services
- **API Layer**: Controllers, request/response mapping, SSE setup
