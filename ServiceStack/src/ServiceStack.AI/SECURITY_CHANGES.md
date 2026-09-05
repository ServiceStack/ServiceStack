# Security & Hardening Changes: ServiceStack.AI

## Overview
This document summarizes the resource lifecycle hardening, memory leak remediation, null safety validations, and robust error handling implemented in `ServiceStack.AI`.

---

### 1. Temporary File Leak in `NodeTypeChat`
- **Severity**: High / Resource Exhaustion
- **Issue**:
  - In `NodeTypeChat.TranslateMessageAsync`, when `request.SchemaPath == null`, a temporary schema file was allocated via `Path.GetTempFileName()`.
  - If process execution timed out, was cancelled, failed to spawn, or reported an error, the execution exited before reaching `File.Delete(schemaPath)`.
  - Repeated execution failures under load or adversarial inputs resulted in accumulated temporary schema files consuming OS disk storage and inode limits.
- **Remediation**:
  - Encapsulated schema file generation and execution in a `try ... finally` block, ensuring `File.Delete(schemaPath)` is unconditionally invoked for generated temporary schema files upon method exit.

---

### 2. Thread-Static Buffer Cache Leak in `NodeTypeChat` and `WhisperLocalSpeechToText`
- **Severity**: Medium / Memory Leak & Thread-Static Cache Starvation
- **Issue**:
  - `NodeTypeChat` and `WhisperLocalSpeechToText` allocated cached `StringBuilder` instances via `StringBuilderCache.Allocate()` and `StringBuilderCacheAlt.Allocate()`.
  - If `ProcessUtils.RunAsync` threw an exception (e.g., timeout, process start failure), the method aborted prior to invoking `ReturnAndFree()`.
  - This prevented the allocated builders from returning to their thread-static cache slots, permanently leaking allocated buffers and causing cache starvation.
- **Remediation**:
  - Wrapped `ProcessUtils.RunAsync` in a `try ... finally` block, guaranteeing that `StringBuilderCache.ReturnAndFree(sb)` and `StringBuilderCacheAlt.ReturnAndFree(sbError)` are always called even during unhandled exceptions or cancellations.

---

### 3. Socket & Connection Leak in `WhisperApiSpeechToText`
- **Severity**: High / Socket Exhaustion & Connection Lifecycle Defect
- **Issue**:
  - `WhisperApiSpeechToText.TranscribeAsync` created a new `HttpClient` on every transcription request without disposing it or providing an injection hook.
  - Under frequent transcription requests, this led to TCP socket starvation and connection leaks.
  - Additionally, setting `client.DefaultRequestHeaders.Authorization` on shared clients introduced thread safety risks across concurrent invocations.
- **Remediation**:
  - Added an optional `HttpClient? HttpClient { get; set; }` property allowing consumers and dependency injection to reuse existing HTTP clients and sockets.
  - When creating a fallback client, encapsulated its lifecycle within a `try ... finally` block with guaranteed disposal.
  - Migrated authorization from `client.DefaultRequestHeaders` to per-request `HttpRequestMessage.Headers.Authorization`, ensuring thread safety during concurrent operations.
  - Added safe URI combining via `BaseUri.CombineWith("audio/transcriptions")` to eliminate malformed URLs caused by trailing/leading slashes.

---

### 4. Semantic Kernel Named Service Resolution in `KernelTypeChat`
- **Severity**: Medium / Functional & Configuration Defect
- **Issue**:
  - `KernelTypeChat` exposed a `ServiceId` property, but `TranslateMessageAsync` resolved the chat completion service with `Kernel.GetRequiredService<IChatCompletionService>()`, ignoring `ServiceId`.
  - Applications configuring multiple chat services (e.g., OpenAI, Azure OpenAI, Anthropic) within the same `Kernel` could not resolve the specific targeted service.
- **Remediation**:
  - Updated `TranslateMessageAsync` to resolve `Kernel.GetRequiredService<IChatCompletionService>(ServiceId)` when `ServiceId` is configured.
  - Added explicit null validation on constructor arguments and request objects.

---

### 5. Defensive Parameter Validation and Null Safety
- **Severity**: Low / Robustness & Input Validation
- **Issue**:
  - Missing null checks on virtual file operations and HTTP multipart helper methods led to raw `NullReferenceException` errors.
  - An empty `fileName` passed to `AddFileInfo` caused downstream exceptions in MIME type lookups.
  - Unsafe JSON type casts (`(Dictionary<string, object>)JSON.parse(...)`) crashed on non-dictionary or error responses.
- **Remediation**:
  - Added `ArgumentNullException.ThrowIfNull` guards across `HttpClientUtils`, `KernelTypeChat`, `NodeTypeChat`, `WhisperApiSpeechToText`, and `WhisperLocalSpeechToText`.
  - Defaulted empty file names to `"file"` in `AddFileInfo`.
  - Replaced unsafe casts with type-safe pattern matching (`JSON.parse(...) is Dictionary<string, object> obj`).
  - Added informative `InvalidOperationException` when required API credentials are missing.
