# CONTEXT: ServiceStack.AI

## Purpose

`ServiceStack.AI` provides AI capabilities and integrations for ServiceStack applications, bridging AI models, speech-to-text systems, and structured type translators.

Key capabilities include:
- **Speech-to-Text (`ISpeechToText`)**: Transcribing audio to text via cloud APIs (`WhisperApiSpeechToText`) or local offline models (`WhisperLocalSpeechToText`).
- **Structured LLM Translation (`ITypeChat`)**: Converting natural language into strongly-typed DTO responses conforming to TypeScript/C# schemas using Microsoft Semantic Kernel (`KernelTypeChatProvider`) or Node.js TypeChat (`NodeTypeChatProvider`).
- **Microsoft Semantic Kernel Integration**: Leveraging Semantic Kernel's multi-provider chat completion, plugins, and agents.

**Target frameworks**: `net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Interfaces     ServiceStack.Text     ServiceStack.Common
         │                          │                     │
         └──────────────────────────┼─────────────────────┘
                                    ▼
                             ServiceStack.AI
                                    │
           ┌────────────────────────┴────────────────────────┐
           ▼                                                 ▼
AI Chatbots & Agent Orchestration             Voice & Speech Processing Services
(ServiceStack.AI.Chat, Locode AI)             (Whisper Transcriptions)
```

- **Depends on**: `ServiceStack.Interfaces`, `ServiceStack.Text`, `ServiceStack.Common`, `Microsoft.SemanticKernel`.
- **Depended on by**: `ServiceStack.AI.Chat`, AI agent plugins, AutoQuery AI features, and developer applications building intelligent services.
- **Related Packages**: `ServiceStack.AI.Chat` (provides full-blown durable multi-agent chat workspaces and web UI).

---

## Key Functionality

### 1. Speech-to-Text (`ISpeechToText`)
| Class | Description |
|---|---|
| `WhisperApiSpeechToText` | Transcribes audio via the OpenAI Whisper REST API (`/v1/audio/transcriptions`). Reuses injected `HttpClient` instances and attaches Bearer tokens per-request. |
| `WhisperLocalSpeechToText` | Runs local `whisper` CLI executables via `ProcessUtils.RunAsync`, supporting offline, zero-cloud transcription. |

### 2. TypeChat Structured Output (`ITypeChat`)
| Class | Description |
|---|---|
| `KernelTypeChatProvider` | Uses Microsoft Semantic Kernel `IChatCompletionService` to translate natural language into validated structured JSON models matching target schemas. Supports named service resolution (`ServiceId`). |
| `NodeTypeChatProvider` | Drives TypeScript TypeChat CLI via Node.js child processes, generating and validating temporary schema files. |

### 3. HTTP & Utility Helpers
- `HttpClientUtils`: Multipart form serialization and HTTP request dispatchers for AI payloads.

---

## Architecture & Design Patterns

### Process-Based vs. In-Process Execution
- `WhisperLocalSpeechToText` and `NodeTypeChatProvider` use out-of-process CLI runners via `ProcessUtils.RunAsync`.
- `WhisperApiSpeechToText` and `KernelTypeChatProvider` run fully in-process via managed HTTP clients and Semantic Kernel abstractions.

### Transient Buffer & Process Management
Process output capture relies on `StringBuilderCache`. All temporary files and thread-static buffers are strictly guarded by `try ... finally` blocks to ensure cleanup under cancellation or process timeouts.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Always enforce these rules:

### 1. Temporary File Cleanup (`NodeTypeChat`)
- **Risk**: Generating temporary schema files via `Path.GetTempFileName()` without `finally` blocks will leak files on process timeouts or start failures, filling disk space and exhausting OS inodes.
- **Rule**: Temporary schema files must be cleaned up in a `finally` block:
  ```csharp
  try { /* execute */ }
  finally { if (tempCreated) File.Delete(schemaPath); }
  ```

### 2. Thread-Static Buffer Cache Starvation
- **Risk**: Aborting `ProcessUtils.RunAsync` without calling `StringBuilderCache.ReturnAndFree` leaves thread-static buffers leaked.
- **Rule**: Always wrap buffer usage in `try ... finally` to guarantee `ReturnAndFree` is executed.

### 3. Socket Leaks & Thread-Safe Headers (`WhisperApiSpeechToText`)
- **Risk**: Creating new `HttpClient` instances per request leads to socket exhaustion. Mutating `client.DefaultRequestHeaders` causes data races across concurrent requests.
- **Rule**: Allow injecting shared `HttpClient` instances. Set authentication headers per-request on `HttpRequestMessage.Headers.Authorization`, not on `HttpClient.DefaultRequestHeaders`.

### 4. Named Semantic Kernel Service Resolution
- In `KernelTypeChatProvider`, always resolve `IChatCompletionService` with `ServiceId` (`Kernel.GetRequiredService<IChatCompletionService>(ServiceId)`) when specified, supporting multi-model configurations.

---

## Common Modification Scenarios

1. **Adding a New Speech-to-Text Provider**
   - Implement `ISpeechToText` and `ISpeechToTextAsync`.
   - Ensure audio streams or files are disposed cleanly.

2. **Adding Support for New AI Providers / Semantic Kernel Models**
   - Configure Semantic Kernel builders with OpenAI, Azure OpenAI, Anthropic, or Ollama connectors.
   - Register the configured `Kernel` in the ServiceStack IoC container.

3. **Modifying TypeChat Schemas**
   - Maintain JSON/TypeScript schema generation parity in `NodeTypeChatProvider` and `KernelTypeChatProvider`.
   - Ensure proper escaping and bounds checking on generated schema strings.
