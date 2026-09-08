# CONTEXT: ServiceStack.Azure

## Purpose

`ServiceStack.Azure` provides integrations with Microsoft Azure cloud services for ServiceStack applications.

Key capabilities include:
- **Azure Service Bus Messaging (`ServiceBusMqServer`)**: ServiceStack MQ broker host utilizing Azure Service Bus topics and queues for asynchronous messaging.
- **Azure Blob Storage VFS (`AzureBlobVirtualFiles`)**: Read/write `IVirtualFiles` implementation allowing ServiceStack to mount Azure Blob Storage containers as virtual directories.
- **Azure Table Storage Cache (`AzureTableCacheClient`)**: Distributed `ICacheClient` backed by Azure Data Tables (`Azure.Data.Tables`).
- **Azure Cognitive Services Speech (`AzureSpeechToText`)**: Enterprise speech recognition and transcription implementing `ISpeechToText`.

**Target frameworks**: `net472;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack (Core)         Azure SDK (Azure.* packages)
         │                               │
         └───────────────┬───────────────┘
                         ▼
                ServiceStack.Azure
                         │
    ┌────────────────────┼────────────────────┐
    ▼                    ▼                    ▼
ServiceBusMqServer  AzureBlobVirtualFiles  AzureTableCacheClient
(Azure Service Bus)  (Blob Storage VFS)     (Table Storage Cache)
```

- **Depends on**: `ServiceStack`, `ServiceStack.Interfaces`, `ServiceStack.Common`, `ServiceStack.Client`, `ServiceStack.Text`, Azure SDK (`Azure.Messaging.ServiceBus`, `Azure.Storage.Blobs`, `Azure.Data.Tables`, `Microsoft.CognitiveServices.Speech`).
- **Depended on by**: ServiceStack applications hosted in Microsoft Azure (App Services, AKS, Container Apps) using Azure managed cloud services.
- **Related Cloud Providers**: `ServiceStack.Aws`, `ServiceStack.GoogleCloud`.

---

## Key Functionality

### Primary Types
| Class | Role |
|---|---|
| `ServiceBusMqServer` | `IMessageService` host processing ServiceStack service requests from Azure Service Bus queues and topics. |
| `AzureBlobVirtualFiles` | `IVirtualFiles` provider mapping Azure Blob containers to virtual files and directories. |
| `AzureTableCacheClient` | Implements `ICacheClient` and `ICacheClientAsync` using Azure Table Storage with TTL support. |
| `AzureSpeechToText` | Implements `ISpeechToText` using Azure Cognitive Services Speech SDK. |

---

## Architecture & Design Patterns

### Modern Azure SDK Integration
Built on modern `Azure.*` client libraries (`Azure.Messaging.ServiceBus`, `Azure.Storage.Blobs`), supporting managed identity (`TokenCredential`), connection strings, and asynchronous streaming.

### Path Sanitization in Blob Hierarchies
Virtual blob paths normalize forward slashes and resolve `..` segments to ensure directory boundaries are respected within Azure Blob containers.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Follow these precautions:

### 1. Never Disable Deserialization Whitelists Globally
- **Risk**: Assigning `JsConfig.AllowRuntimeType = _ => true;` globally disables type whitelist checks across the entire host application, allowing arbitrary types to be instantiated via JSON `__type` properties.
- **Rule**: Never mutate `JsConfig.AllowRuntimeType` globally. Standard `ServiceStack.Messaging` namespaces and interfaces are already safely allowed by default.

### 2. Lock Token Cleanup in Message Handling
- In `ServiceBusMqWorker.HandleMessageAsync`, always clean up `factory.pendingAcks.TryRemove(msg.LockToken, out _)` in a `finally` block to prevent memory leaks from completed messages.

### 3. Path Canonicalization in Blob VFS
- In `AzureBlobVirtualFilesHelpers.SanitizePath`, resolve relative `..` segments via `.ResolvePaths()` to prevent directory traversal across blob hierarchies.

### 4. ReDoS Timeouts on Regex Lookups
- In `AzureTableCacheClient`, always pass `RegexTimeout` (`TimeSpan.FromSeconds(2)`) to regex queries to avoid catastrophic backtracking.

---

## Common Modification Scenarios

1. **Mounting Azure Blob Storage as Virtual Files**
   ```csharp
   var blobServiceClient = new BlobServiceClient(connectionString);
   VirtualFiles = new AzureBlobVirtualFiles(blobServiceClient, "my-container");
   ```

2. **Registering Azure Service Bus MQ**
   ```csharp
   container.Register<IMessageService>(c => 
       new ServiceBusMqServer(connectionString));
   var mqServer = container.Resolve<IMessageService>();
   mqServer.RegisterHandler<MyRequest>(ExecuteMessage);
   mqServer.Start();
   ```
