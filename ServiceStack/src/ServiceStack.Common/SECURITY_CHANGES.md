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
