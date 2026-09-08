# CONTEXT: ServiceStack.Aws

## Purpose

`ServiceStack.Aws` provides comprehensive Amazon Web Services (AWS) integrations and cloud-native adapters for ServiceStack.

Key capabilities include:
- **PocoDynamo**: High-performance, code-first POCO client for Amazon DynamoDB with typed LINQ query support and automated table generation.
- **S3 Virtual File System (`S3VirtualFiles`, `R2VirtualFiles`)**: Read/write `IVirtualFiles` implementation over Amazon S3 and Cloudflare R2 buckets for media, upload, and static asset storage.
- **SQS Message Queue Server (`SqsMqServer`)**: ServiceStack message broker host executing service requests asynchronously over Amazon SQS.
- **DynamoDB Providers**: `DynamoDbAuthRepository` (user identity and OAuth tokens), `DynamoDbCacheClient` (distributed caching), and `DynamoDbAppSettings` (centralized configuration).
- **Speech Transcription**: Amazon Transcribe service integration.

**Target frameworks**: `net472;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack (Core)       AWSSDK (.NET v4 SDK)
         │                         │
         └────────────┬────────────┘
                      ▼
               ServiceStack.Aws
                      │
    ┌─────────────────┼─────────────────┬─────────────────┐
    ▼                 ▼                 ▼                 ▼
PocoDynamo       S3VirtualFiles    SqsMqServer    DynamoDb Providers
(DynamoDB ORM)   (S3 / R2 Blobs)   (SQS Broker)   (Auth, Cache, Config)
```

- **Depends on**: `ServiceStack`, `ServiceStack.Interfaces`, `ServiceStack.Common`, `ServiceStack.Client`, `ServiceStack.Text`, AWSSDK (`AWSSDK.DynamoDBv2`, `AWSSDK.S3`, `AWSSDK.SQS`, `AWSSDK.TranscribeService`).
- **Depended on by**: ServiceStack applications deployed on AWS (ECS, Lambda, EC2) or utilizing AWS cloud services for storage, database, and messaging.
- **Related Cloud Providers**: `ServiceStack.Azure` (Azure services), `ServiceStack.GoogleCloud` (GCP services).

---

## Key Functionality

### Primary Types
| Class | Role |
|---|---|
| `PocoDynamo` | Declarative DynamoDB POCO client with LINQ expressions, secondary index queries, and auto-table creation. |
| `S3VirtualFiles` | `IVirtualFiles` provider mapping Amazon S3 bucket paths to ServiceStack virtual file systems. |
| `R2VirtualFiles` | S3-compatible provider optimized for Cloudflare R2 storage. |
| `SqsMqServer` | `IMessageService` host receiving messages from Amazon SQS queues and routing them to ServiceStack services. |
| `DynamoDbAuthRepository` | Implements `IUserAuthRepository` and `IUserAuthRepositoryAsync` backed by DynamoDB tables. |
| `DynamoDbCacheClient` | Implements `ICacheClient` and `ICacheClientAsync` backed by DynamoDB with TTL expiration. |

---

## Architecture & Design Patterns

### Code-First DynamoDB Mapping
`PocoDynamo` maps C# POCO models to DynamoDB items using conventions and annotations (`[HashKey]`, `[RangeKey]`, `[GlobalSecondaryIndex]`).

### S3 Partial Streaming & Range Requests
`S3VirtualFile` supports HTTP range requests and chunked streaming (`WritePartialToAsync`) for media and large file distribution.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Always preserve these fixes:

### 1. No Silent Error Swallowing on Storage Uploads
- In `R2VirtualFiles.WriteFileAsync` and `S3VirtualFiles`, upload exceptions must never be caught and swallowed. Always rethrow (`throw;`) so callers detect failed uploads and avoid silent data loss.

### 2. Username Validation & Email Fallback in DynamoDB Auth
- In `DynamoDbAuthRepository.ValidateNewUser`, usernames must not contain `@`.
- When querying `GetUserAuthByUserName` with an email address (`@`), fallback to email attribute scanning if the username index produces no match.

### 3. S3 Socket and Response Disposal
- `S3VirtualFile.WritePartialToAsync` must wrap `GetObjectResponse` in a `using` block to prevent unclosed HTTP responses and connection pool starvation.

### 4. Cache Key Existence vs. Default Values
- In `DynamoDbCacheClient.CacheAdd` and `CacheReplace`, check `GetCacheEntry(key) != null` rather than comparing against `default(T)`, ensuring value types (`0`, `false`) are not mistakenly treated as non-existent keys.

### 5. Path Canonicalization
- `S3VirtualFiles.SanitizePath` must resolve relative segments (`..`) via `.ResolvePaths()` to prevent directory traversal across bucket prefixes.

---

## Common Modification Scenarios

1. **Configuring S3 as Virtual File System**
   ```csharp
   var s3Client = new AmazonS3Client(awsCredentials, RegionEndpoint.USEast1);
   VirtualFiles = new S3VirtualFiles(s3Client, "my-bucket-name");
   ```

2. **Registering DynamoDB Authentication & Caching**
   ```csharp
   var dynamoClient = new AmazonDynamoDBClient(awsCredentials, RegionEndpoint.USEast1);
   var db = new PocoDynamo(dynamoClient);
   db.RegisterTable<UserAuth>();
   db.InitSchema();
   container.Register<IUserAuthRepository>(c => new DynamoDbAuthRepository(db));
   ```
