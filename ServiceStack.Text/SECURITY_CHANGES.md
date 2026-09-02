# Security Changes & Remediation Reference (`ServiceStack.Text`)

This document details security vulnerabilities identified and remediated in `ServiceStack.Text`. It describes the nature of each finding, the changes introduced, the secure defaults applied, and the configuration options available to revert or customize behavior if required for legacy compatibility.

---

## 1. Untrusted Type Instantiation via `[Serializable]`
- **Severity**: High (Remote Code Execution / Deserialization)
- **Description**: `JsConfig.AllowRuntimeTypeWithAttributesNamed` previously included `nameof(SerializableAttribute)`. Many built-in .NET Framework classes are annotated with `[Serializable]` (e.g., `System.IO.FileInfo`, `System.Diagnostics.Process`), allowing potential arbitrary object instantiation during polymorphic deserialization.
- **Change**: Removed `[Serializable]` (`SerializableAttribute`) from the default allowed runtime type attributes. Allowed attributes now default strictly to `[RuntimeSerializable]` and `[DataContract]`.
- **Reversion / Migration**:
  If your codebase relies on `[Serializable]` for polymorphic DTOs, decorate your DTOs with `[RuntimeSerializable]` or `[DataContract]`, or re-add `[Serializable]` to your global configuration on startup:
  ```csharp
  JsConfig.AllowRuntimeTypeWithAttributesNamed.Add(nameof(SerializableAttribute));
  ```

---

## 2. Stack Overflow Denial of Service (DoS) Recursion Limits
- **Severity**: High (Process Crash / Denial of Service)
- **Description**: Deeply nested JSON or JSV payloads (or payloads referencing cyclical structures) could cause unrestricted recursive calls during deserialization, resulting in an uncatchable `StackOverflowException` that terminates the host process.
- **Change**: Added deserialization depth tracking across all JSON, JSV, collection, and dictionary parsers. When nested object depth exceeds `JsConfig.MaxDepth` (default: `50`), a `SerializationException` is thrown before a stack overflow can occur.
- **Reversion / Configuration**:
  To adjust the maximum allowed recursion depth:
  ```csharp
  JsConfig.MaxDepth = 100; // Increase depth limit if deep hierarchies are required
  ```

---

## 3. Ambient Windows Credentials Sent by Default in `HttpUtils.HttpClient`
- **Severity**: Medium (Credential Exposure / NTLM Reflection)
- **Description**: `HttpUtils.HttpClientHandlerFactory` previously initialized `HttpClientHandler` with `UseDefaultCredentials = true`. This caused HTTP requests to automatically transmit current Windows identity tokens (NTLM / Kerberos) to remote HTTP endpoints by default.
- **Change**: Changed `UseDefaultCredentials` default to `false` in `HttpUtils.HttpClientHandlerFactory`.
- **Reversion / Opt-In**:
  To re-enable ambient Windows credentials for intranet or domain environments:
  ```csharp
  HttpUtils.HttpClientHandlerFactory = () => new HttpClientHandler {
      UseDefaultCredentials = true,
      AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.Deflate | DecompressionMethods.GZip,
  };
  ```

---

## 4. XML Reader Settings / DTD Prohibition Bypass in `XmlSerializer`
- **Severity**: High (XXE / SSRF / Billion Laughs DoS)
- **Description**: `XmlSerializer.DeserializeFromStream` directly called `DataContractSerializer.ReadObject(Stream)` without passing an `XmlReader` configured with `XmlReaderSettings`. This bypassed DTD prohibitions and allowed external entity resolution (XXE) and XML entity expansion attacks.
- **Change**: `XmlSerializer.DeserializeFromStream` now creates and wraps the stream in an `XmlReader` using `XmlSerializer.XmlReaderSettings`, which prohibits DTD processing and enforces `MaxCharactersInDocument` limits.
- **Reversion / Configuration**:
  To customize XML reader settings:
  ```csharp
  XmlSerializer.XmlReaderSettings.DtdProcessing = DtdProcessing.Prohibit;
  XmlSerializer.XmlReaderSettings.MaxCharactersInDocument = 1024 * 1024;
  ```

---

## 5. String Index & Scope Desync on Escaped JSON Keys in `JsonTypeSerializer`
- **Severity**: Medium (Parser Desync / Broken Property Parsing)
- **Description**: In `JsonTypeSerializer.UnescapeJsString`, when unescaping JSON object keys containing escaped characters (e.g., `\"`), index advancement was not properly synchronized with token boundaries, resulting in misaligned property names and subsequent parser errors.
- **Change**: Updated `UnescapeJsString` to accurately unescape string tokens up to matching delimiters and return the exact advanced parser index.

---

## 6. Regular Expression Denial of Service (ReDoS) in Markup Stripping
- **Severity**: Medium (CPU Starvation / Denial of Service)
- **Description**: `StringExtensions.StripHtml` and `StripMarkdownMarkup` used regular expressions with polynomial backtracking patterns (`<(.|\n)*?>`, `\[(.|\n)*?\]`, `\((.|\n)*?\)`). Supplying unclosed markup tags (e.g., `<` followed by thousands of characters) caused catastrophic backtracking and CPU starvation.
- **Change**: Replaced backtracking regexes with non-backtracking character-class patterns (`<[^>]*>`, `\[[^\]]*\]`, `\([^\)]*\)`) with explicit 1-second match timeouts.

---

## 7. Spreadsheet Formula / CSV Injection (CWE-1236) in `CsvWriter`
- **Severity**: Medium (Client-Side Code Execution / Formula Injection)
- **Description**: Exporting user-controlled data to CSV without sanitization could allow spreadsheet formula injection. When opened in Microsoft Excel or Google Sheets, cells beginning with `=`, `+`, `-`, `@`, `\t`, or `\r` could execute formulas or external commands.
- **Change**: Added `CsvConfig.EscapeFormulas` (enabled by default as `true`). Non-numeric cells starting with dangerous formula characters are automatically prefixed with a single quote (`'`) to ensure they are treated as plain text by spreadsheet viewers. `FromCsvField` automatically unescapes the single quote when reading.
- **Reversion / Opt-Out**:
  To disable CSV formula escaping and export raw formulas:
  ```csharp
  CsvConfig.EscapeFormulas = false;
  ```

---

## 8. Unbounded Cache Growth in `AssemblyUtils.TypeCache`
- **Severity**: Low / Medium (Memory Leak / Denial of Service)
- **Description**: `AssemblyUtils.UncheckedFindType` cached both found types and missing types (`null`) in a single unbounded dictionary. Malicious requests sending randomly generated type names in `__type` could fill the cache indefinitely, resulting in an `OutOfMemoryException`.
- **Change**: Separated negative lookups into a dedicated `NegativeTypeCache` capped at `MaxNegativeCacheSize` (1,000 entries). Found types remain cached in `TypeCache`.

---

## 9. Dynamic Proxy Parameter Type Reflection Bug
- **Severity**: Low / Correctness (Type Resolution Failure)
- **Description**: In `DynamicProxy.BindMethod`, parameter types for dynamic proxy methods were obtained using `p.GetType()` (which returns `System.Reflection.RuntimeParameterInfo`) rather than `p.ParameterType`. This caused dynamic proxy generation to fail for interfaces with parameterized methods.
- **Change**: Fixed parameter type reflection to use `p.ParameterType`.

---

## 10. Native Cryptographic Handle Leak in `LicenseUtils.VerifySignedHash`
- **Severity**: Low (Unmanaged Resource / Handle Leak)
- **Description**: `LicenseUtils.VerifySignedHash` allocated an instance of `RSACryptoServiceProvider` without disposing it, leaking underlying native OS cryptographic handles (CryptoAPI / CNG / OpenSSL) until finalization.
- **Change**: Wrapped `RSACryptoServiceProvider` in a `using` statement to guarantee immediate deterministic disposal of native cryptographic handles.

---

## 11. Unencoded Query Parameter Key in `HttpUtils.AddQueryParam`
- **Severity**: Low (Query Parameter Injection / Corruption)
- **Description**: `HttpUtils.AddQueryParam(url, key, val, encode: true)` URL-encoded `val`, but left `key` raw and unencoded, unlike `AddQueryParams` and `AddNameValueCollection`.
- **Change**: `AddQueryParam` now URL-encodes both `key` and `val` when `encode` is `true`.

---

## 12. Path Traversal Semantics in `PathUtils.ResolvePaths`
- **Severity**: Low / Correctness (Path Canonicalization)
- **Description**: `PathUtils.ResolvePaths` did not distinguish between rooted and relative paths during `..` resolution. In some cases, consecutive `..` segments could incorrectly cancel preceding relative segments or allow traversal past root.
- **Change**: Updated `ResolvePaths` to prevent rooted paths and URLs from traversing above their root while properly preserving legitimate leading relative `..` segments.
