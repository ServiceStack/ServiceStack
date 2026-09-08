# CONTEXT: ServiceStack.GoogleCloud

## Purpose

`ServiceStack.GoogleCloud` provides integrations with Google Cloud Platform (GCP) services, tailored for ServiceStack architectures.

Key features include:
- **Google Cloud Storage Virtual File System (`GoogleCloudVirtualFiles`)**: A full read/write `IVirtualFiles` implementation allowing ServiceStack to treat GCS buckets as virtual file providers (e.g. for static asset serving, file uploads, or template storage).
- **Google Cloud Speech-to-Text (`GoogleCloudSpeechToText`)**: Enterprise speech recognition and transcription adapter implementing `ISpeechToText` using Google Cloud Speech V2.
- **Unified GCP Configuration (`GoogleCloudConfig`)**: Service account authentication, credential verification, and bucket mapping.

**Target frameworks**: `net472;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Interfaces     ServiceStack.Common     ServiceStack.Text
         │                          │                       │
         └──────────────────────────┼───────────────────────┘
                                    ▼
                        ServiceStack.GoogleCloud
                                    │
           ┌────────────────────────┴────────────────────────┐
           ▼                                                 ▼
Google Cloud Storage (VFS Provider)          Google Cloud Speech-to-Text (AI/Audio)
(Buckets as IVirtualFiles)                   (Speech recognition via Cloud Speech V2)
```

- **Depends on**: `ServiceStack.Interfaces`, `ServiceStack.Common`, `ServiceStack.Text`, `Google.Cloud.Storage.V1`, `Google.Cloud.Speech.V2`.
- **Depended on by**: ServiceStack applications hosted in Google Cloud or utilizing GCS for blob/media storage and speech recognition.
- **Related VFS Providers**: `FileSystemVirtualFiles` (local disk in `ServiceStack.Common`), `S3VirtualFiles` (in `ServiceStack.Aws`), `AzureBlobVirtualFiles` (in `ServiceStack.Azure`).

---

## Key Functionality

### 1. Google Cloud Storage VFS Provider
| Class | Role |
|---|---|
| `GoogleCloudVirtualFiles` | Root `IVirtualPathProvider` and `IVirtualFiles` managing GCS bucket operations (read, write, delete, enumerate). |
| `GoogleCloudVirtualDirectory` | Represents virtual directory hierarchies in GCS object prefixes. |
| `GoogleCloudVirtualFile` | Represents a single object in a GCS bucket, providing stream reading, metadata, and MIME type mapping. |

### 2. Google Cloud Speech Recognition
- **`GoogleCloudSpeechToText`**: Implements `ISpeechToText` / `ISpeechToTextAsync` using `Google.Cloud.Speech.V2.SpeechClient`.
- Supports audio transcription with custom phrases, model tuning, and phrase sets (`PhraseWeights`, `PhraseSetId`).

---

## Architecture & Design Patterns

### VFS Path Normalization & Prefix Hierarchies
Because GCS is an object store with flat keys, directories are modeled via key prefixes (`/`). All paths are sanitized to use forward slashes without leading `/` to conform to GCP object key conventions.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Always preserve these safety patterns:

### 1. Directory Boundary Preservation on Uploads
- **Risk**: In `GoogleCloudVirtualDirectory.AddFile`, writing files without prepending `DirPath` places files into the bucket root instead of the subdirectory.
- **Rule**: Always delegate directory uploads through `PathProvider.WriteFile` with `DirPath.CombineWith(filePath)`.

### 2. Graceful 404 Handling on Object Lookups
- **Risk**: The Google Cloud Storage client throws a `GoogleApiException` with HTTP 404 when an object is not found, unlike file system providers that return `null`.
- **Rule**: Ensure object queries catch `GoogleApiException` with 404 status and return `null` rather than propagating unhandled exceptions.

### 3. Path Sanitization & Cross-Platform Slashes
- In `GoogleCloudVirtualFiles.SanitizePath`, replace backslashes (`\`) with forward slashes (`/`) *before* stripping leading separators to prevent malformed object keys on Windows paths.

### 4. Credential Verification
- `GoogleCloudConfig.AssertValidCredentials()` must throw specific exceptions (`FileNotFoundException`, `InvalidOperationException`) instead of generic `System.Exception` when service account credentials or environment variables are missing.

---

## Common Modification Scenarios

1. **Mounting a GCS Bucket as VFS Provider**
   ```csharp
   var gcsClient = StorageClient.Create();
   VirtualFiles = new GoogleCloudVirtualFiles(gcsClient, "my-gcs-bucket");
   ```

2. **Transcribing Audio via Google Cloud Speech**
   - Inject `ISpeechToText` and invoke `TranscribeAsync(audioStream)`.
   - Ensure `RecognizerId` and credentials are valid in `GoogleCloudConfig`.
