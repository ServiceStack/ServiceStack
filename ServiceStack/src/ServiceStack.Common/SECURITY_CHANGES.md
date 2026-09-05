# Security Changes & Remediation Reference (`ServiceStack.Common`)

This document details security, robustness, and stability fixes across `ServiceStack.Common`.

---

## 1. Process Path Resolution & ArgumentOutOfRange Crash Prevention (`ProcessUtils` & `ProtectedScripts`)
- **Severity**: Medium (Crash / False Null Failure)
- **Description**:
  - `ProcessUtils.FindExePath` and `ProtectedScripts.exePath` parsed the output of `which` or `where` using:
    `output.Substring(0, output.IndexOf(Environment.NewLine, StringComparison.Ordinal))`
  - When child process output did not end with `Environment.NewLine` (e.g. single-line output without CRLF/LF, or on systems where output uses `\n` instead of `\r\n`), `IndexOf` returned `-1`, throwing `ArgumentOutOfRangeException`.
  - Swallowed by `catch {}`, this caused `FindExePath` and `exePath` to return `null` even when the target executable was present on the system.
- **Change**:
  - Changed output parsing to safely read lines (`output.ReadLines().FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim()`).
  - Consolidated `ProtectedScripts.exePath` to delegate directly to `ProcessUtils.FindExePath`.

---

## 2. Exponential Backoff Bitwise XOR Calculation Fix (`VirtualPathUtils`)
- **Severity**: Medium (Retry Scheduling Defect)
- **Description**:
  - `VirtualPathUtils.SleepBackOffMultiplier` computed retry delay as:
    `var nextTryMs = (2 ^ i) * 50;`
  - In C#, `^` is bitwise XOR, not exponentiation. This produced non-exponential delays (100ms, 150ms, 0ms, 50ms...) where retry #2 had 0ms delay.
- **Change**:
  - Corrected formula to `(1 << Math.Min(i, 10)) * 50`, providing true exponential backoff while capping shift bounds.

---

## 3. Directory Enumeration Bounds Safety (`FileSystemVirtualDirectory`)
- **Severity**: Low / Crash Prevention
- **Description**:
  - In `FileSystemVirtualDirectory.EnumerateDirectories(string dirName)`, accessing `dirName[dirName.Length - 1]` without checking for empty string threw an unhandled `IndexOutOfRangeException` when `dirName == ""`.
- **Change**:
  - Added null/empty check: `!string.IsNullOrEmpty(dirName) && dirName[dirName.Length - 1] == ':'`.

---

## 4. Null Safety & Non-Element Node Skipping (`XLinqExtensions`)
- **Severity**: Low / Robustness
- **Description**:
  - `XLinqExtensions.FirstElement(this XElement element)` evaluated `element.FirstNode.NodeType == XmlNodeType.Element`, throwing `NullReferenceException` if `element` was null or had no child nodes.
  - Furthermore, it failed to return the first child element if the first node was a comment or whitespace.
- **Change**:
  - Iterated child nodes (`for (var node = element?.FirstNode; node != null; node = node.NextNode)`) to safely locate the first `XElement` or return `null`.

---

## 5. Generic Type Argument Slicing Bounds Safety (`ProtectedScripts`)
- **Severity**: Low / Robustness
- **Description**:
  - `ProtectedScripts.typeGenericArgs` assumed type strings always contained `<` and `>`, executing `argList.Substring(0, argList.Length - 1)` on the result of `typeName.RightPart('<')`. For non-generic or malformed type strings, this caused string truncation or `ArgumentOutOfRangeException`.
- **Change**:
  - Added explicit boundary check for `<` and `>` before extracting and splitting generic arguments.

---

## 6. Path Normalization with Leading Slashes (`AbstractVirtualPathProviderBase`)
- **Severity**: Low / Path Normalization
- **Description**:
  - `AbstractVirtualPathProviderBase.SanitizePath` only checked `filePath[0] == '/'` when stripping leading separators, causing paths with leading backslashes (`\`) to retain a double leading slash (`//path`) after backslash replacement.
- **Change**:
  - Updated to `filePath.TrimStart('/', '\\').Replace('\\', '/')`.

---

## 7. Directory Boundary Path Traversal Prevention (`FileSystemVirtualFiles.IsPathSafe`)
- **Severity**: High / Security (Path Traversal / CWE-22)
- **Description**:
  - `FileSystemVirtualFiles.IsPathSafe` validated paths using `fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)` without ensuring directory separator boundaries.
  - When `basePath` is `/var/app`, a relative path such as `../app_secret/file.txt` resolves to `/var/app_secret/file.txt`, which starts with `/var/app` and was falsely reported as safe.
- **Change**:
  - Enforced directory separator boundary checking so `fullPath` must either be identical to `basePath` or start with `basePath` followed by the directory separator character (`Path.DirectorySeparatorChar`).
  - Added null guards for `basePath` and `relativePath`.

---

## 8. MultiVirtualDirectory ParentDirectory `InvalidCastException` Fix (`MultiVirtualFiles`)
- **Severity**: Medium / Crash Prevention
- **Description**:
  - In `MultiVirtualDirectory.ParentDirectory`, `dirs.SelectMany(x => x.ParentDirectory).Cast<IVirtualDirectory>()` was used.
  - Because `IVirtualDirectory` implements `IEnumerable<IVirtualNode>`, calling `SelectMany` enumerated the child nodes inside each parent directory instead of selecting the parent directories themselves.
  - When the parent directory contained files (`IVirtualFile`), the subsequent `.Cast<IVirtualDirectory>()` threw an `InvalidCastException`.
- **Change**:
  - Changed `dirs.SelectMany(...)` to `dirs.Select(x => x.ParentDirectory).Where(x => x != null)`.

---

## 9. MultiVirtualDirectory Child Property Shadowing & Empty Directory Crash (`MultiVirtualFiles`)
- **Severity**: Medium / Incorrect State & Crash
- **Description**:
  - `MultiVirtualDirectory.Name`, `VirtualPath`, `RealPath`, and `LastModified` accessed `this.First().*`.
  - Because `MultiVirtualDirectory` implements `IEnumerable<IVirtualNode>`, `this.First()` evaluated the directory's child nodes, returning the properties of a child node rather than the directory itself. If the multi-directory was empty, `this.First()` threw an `InvalidOperationException`.
- **Change**:
  - Switched property accessors to delegate to `dirs.First().*`.
  - Cloned `virtualPath` stacks in multi-directory iteration so a lookup failure in an earlier provider does not deplete the stack for later providers.

---

## 10. FileSystemMapping Alias Prefix Collision Fix (`FileSystemMapping`)
- **Severity**: Low / Routing Bug
- **Description**:
  - `FileSystemMapping.GetRealVirtualPath` used `virtualPath.StartsWith(Alias)` without separator boundary checks.
  - An alias such as `"docs"` erroneously matched `"documentation/readme.md"`, stripping `"docs"` to produce `"umentation/readme.md"`.
- **Change**:
  - Enforced separator boundary: requires either exact match with `Alias` or prefix match with `Alias + "/"`.

---

## 11. Latent Infinite Recursion Fix in `AbstractVirtualFileBase.ReadAllBytes`
- **Severity**: Medium / StackOverflow Prevention
- **Description**:
  - In `AbstractVirtualFileBase.ReadAllBytes()`, if `GetContents()` returned an unexpected or custom type not matching `ReadOnlyMemory<byte>`, `ReadOnlyMemory<char>`, or `string`, the default fallback was `: ReadAllBytes();`.
  - This caused unconditional infinite recursion leading to a `StackOverflowException`.
- **Change**:
  - Added `byte[]` handling and routed the fallback safely to stream reading via `VirtualPathUtils.ReadAllBytes(this)`.

