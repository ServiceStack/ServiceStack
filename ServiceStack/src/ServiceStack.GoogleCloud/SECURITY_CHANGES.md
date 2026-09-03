# Security Changes & Remediation Reference (`ServiceStack.GoogleCloud`)

This document details security, robustness, and stability fixes across `ServiceStack.GoogleCloud`.

---

## 1. Directory Path Traversal & Isolation in Directory Uploads (`GoogleCloudVirtualDirectory.AddFile`)
- **Severity**: Medium (Directory Isolation / File Placement Bug)
- **Description**:
  - In `GoogleCloudVirtualDirectory.AddFile(string filePath, Stream stream)` and `AddFile(string filePath, string contents)`, the method uploaded files directly to `StripDirSeparatorPrefix(filePath)` without prepending `DirPath`. Calling `dir.AddFile("file.txt", stream)` on a subdirectory instance inadvertently wrote the object into the root of the Google Cloud Storage bucket instead of `dir/file.txt`, violating virtual filesystem directory boundaries.
- **Remediation**:
  - Delegated `AddFile` directly to `PathProvider.WriteFile(DirPath != null ? DirPath.CombineWith(filePath) : filePath, ...)`.
  - Ensures path normalization, directory context preservation, MIME type detection, and input validation are uniformly applied.

---

## 2. Unhandled 404 Exception Prevention (`GoogleCloudVirtualDirectory.GetFile`)
- **Severity**: Low (Robustness / Unhandled Exception)
- **Description**:
  - In `GoogleCloudVirtualDirectory.GetFile(string virtualPath)`, the method called `Client.GetObject(...)` directly without catching `GoogleApiException` (404 Not Found). Unlike filesystem providers that return `null` for non-existent files, Google Cloud Storage client throws a `GoogleApiException` with `HttpStatusCode.NotFound`. Requesting a non-existent file path resulted in an unhandled exception rather than returning `null`.
- **Remediation**:
  - Delegated to `PathProvider.GetFile(DirPath != null ? DirPath.CombineWith(virtualPath) : virtualPath)`, which properly catches 404 `GoogleApiException` and safely returns `null`.

---

## 3. Path Sanitization & Leading Separator Normalization (`GoogleCloudVirtualFiles.SanitizePath`)
- **Severity**: Low (Data Integrity / Object Key Normalization)
- **Description**:
  - In `GoogleCloudVirtualFiles.SanitizePath`, paths with leading backslashes (`\folder\file.txt`) had backslashes replaced with forward slashes *after* the leading slash check. As a result, Windows-style paths resulted in `/folder/file.txt` with an unintended leading slash, creating malformed object keys in Google Cloud Storage.
- **Remediation**:
  - Normalized backslashes (`\`) to forward slashes (`/`) prior to stripping any leading separator, ensuring consistent object key names across platforms.

---

## 4. Null Safety & Input Validation Hardening (`GoogleCloudVirtualFiles`, `GoogleCloudConfig`, `GoogleCloudSpeechToText`)
- **Severity**: Low (Defensive Programming / Stability)
- **Description**:
  - `GoogleCloudVirtualFiles` constructor lacked null argument validation for `client` and `bucketName`.
  - `GoogleCloudConfig.ToSpeechToTextConfig` lacked null argument validation on `net472`.
  - `GoogleCloudConfig.AssertValidCredentials()` threw generic `System.Exception` instead of descriptive `InvalidOperationException` and `FileNotFoundException`.
  - In `GoogleCloudSpeechToText.InitAsync`, calling with `PhraseWeights` without setting `PhraseSetId` or calling with null `RecognizerId` caused `ArgumentNullException` crashes when constructing SDK resource names.
- **Remediation**:
  - Added explicit null checks in `GoogleCloudVirtualFiles` and `GoogleCloudConfig`.
  - Guarded against null `PhraseSetId` and `RecognizerId` in `GoogleCloudSpeechToText.InitAsync`.
  - Replaced generic exceptions with specific standard exception types.
  - Eliminated all nullable reference type warnings (`CS8600`, `CS8602`, `CS8604`, `CS8620`) and obsolete member warnings (`CS0618`).
