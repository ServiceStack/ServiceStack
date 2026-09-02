# Security Changes & Remediation Reference (`ServiceStack.Azure`)

This document details security vulnerabilities identified and remediated in `ServiceStack.Azure`.

---

## 1. Global Insecure Deserialization Bypass in `ServiceBusMqMessageFactory`
- **Severity**: Critical (Global Process-Wide Deserialization Bypass)
- **Description**: `ServiceBusMqMessageFactory` previously assigned `JsConfig.AllowRuntimeType = _ => true;` in its constructor. Because `JsConfig.AllowRuntimeType` is a process-wide static configuration in `ServiceStack.Text`, constructing an Azure Service Bus MQ server globally disabled runtime type validation allowlists across the host application, allowing arbitrary types to be instantiated via JSON `__type` properties in any web API or service.
- **Change**: Removed the global assignment. `ServiceStack.Messaging` namespaces and interfaces are already safely allowed by default in `JsConfig.AllowRuntimeTypeInTypesWithNamespaces` and `AllowRuntimeTypeWithInterfacesNamed`.

---

## 2. Unbounded Memory Leak in `pendingAcks` Dictionary
- **Severity**: Medium (Memory Leak / Denial of Service)
- **Description**: In `ServiceBusMqWorker.HandleMessageAsync`, entries were added to `factory.pendingAcks` for every incoming message. In message handler pipelines where messages are handled and auto-completed without an explicit consumer `client.Ack()` invocation, dictionary entries remained indefinitely, causing unbounded memory growth under continuous high message volumes.
- **Change**: Added a `try / finally` block in `ServiceBusMqWorker.HandleMessageAsync` to deterministically clean up `factory.pendingAcks.TryRemove(msg.LockToken, out _)`.

---

## 3. Directory Traversal Path Canonicalization in `AzureBlobVirtualFilesHelpers.SanitizePath`
- **Severity**: Medium (Path Traversal)
- **Description**: `AzureBlobVirtualFilesHelpers.SanitizePath` and `AzureBlobVirtualFiles.SanitizePath` only trimmed leading forward slashes and normalized backslashes without resolving relative `..` path segments, leaving virtual blob hierarchies vulnerable to path traversal.
- **Change**: Updated `SanitizePath` to normalize and resolve relative directory segments using `.ResolvePaths()`.

---

## 4. ReDoS Protection in `AzureTableCacheClient` Regex Queries
- **Severity**: Low / Medium (Regular Expression Denial of Service)
- **Description**: `AzureTableCacheClient.GetKeysByRegex` and `GetKeysByRegexAsync` constructed compiled `Regex` instances without a match timeout (`matchTimeout`), making regex evaluations vulnerable to catastrophic backtracking when evaluated against cached row keys.
- **Change**: Added `AzureTableCacheClient.RegexTimeout` (`TimeSpan.FromSeconds(2)`) and supplied it to all runtime `Regex` instantiations.
