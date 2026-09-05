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

---

## 12. First Result Inversion Fix in `ExecUtils.ExecAllWithFirstOut`
- **Severity**: High / Logic Bug
- **Description**:
  - `ExecUtils.ExecAllWithFirstOut<T, TReturn>` used `if (!Equals(firstResult, default(TReturn))) { firstResult = result; }`.
  - When callers passed `firstResult` initialized to default (e.g. `null` or `0` or `false`), `firstResult` was NEVER set. If `firstResult` started non-default, every iteration overwrote it, yielding the last result instead of the first.
- **Change**:
  - Tracked whether the first result was set using a boolean flag (`!hasResult`), properly capturing the result of the first successful execution.
  - Safe-guarded all `ExecAll*` catch blocks against null elements by using `instance?.GetType() ?? typeof(T)` for error logging.

---

## 13. Process Callback Duration Integer Truncation (`ProcessUtils.RunAsync`)
- **Severity**: Medium / Diagnostic Inaccuracy
- **Description**:
  - `ProcessUtils.RunAsync` calculated `callbackMs = (callbackTicks / Stopwatch.Frequency) * 1000`.
  - Due to integer division between `long` values, any callback taking less than 1 whole second (10,000,000 ticks) evaluated to `0`, discarding accurate callback durations and distorting process execution timing.
- **Change**:
  - Calculated duration as `(long)((callbackTicks * 1000.0) / Stopwatch.Frequency)`.
  - Ensured `StringBuilderCache` buffers are freed if `process.Start()` fails.

---

## 14. SimpleContainer Singleton Lazy Evaluation & Disposal Leak Fix (`SimpleContainer`)
- **Severity**: High / Resource Leak & Unnecessary Instantiation
- **Description**:
  - In `SimpleContainer.AddSingleton`, `Factory[serviceType] = () => InstanceCache.GetOrAdd(serviceType, factory());` eagerly invoked `factory()` on EVERY call to `Resolve()`.
  - In `SimpleContainer.Dispose()`, `var hold = InstanceCache; InstanceCache.Clear();` cleared `hold` before the loop because `hold` referenced the same dictionary, meaning 0 cached `IDisposable` singletons were ever disposed.
- **Change**:
  - Replaced eager argument evaluation with `() => InstanceCache.GetOrAdd(serviceType, _ => factory())`.
  - Snapshotted instances with `InstanceCache.Values.ToArray()` prior to clearing in `Dispose()`.

---

## 15. Default Fallback Value Discard Fix in `FuncUtils.TryExec<T>`
- **Severity**: Medium / Logic Bug
- **Description**:
  - `FuncUtils.TryExec<T>(Func<T> func, T defaultValue)` caught exceptions and returned `default(T)` instead of the caller's `defaultValue`.
- **Change**:
  - Corrected return value to `defaultValue`.

---

## 16. Chained AppTask Execution Loop Abort Fix (`AppTasks.RanAsTask`)
- **Severity**: Medium / Execution Flow Bug
- **Description**:
  - In `AppTasks.RanAsTask()`, `return exitCode;` was placed unconditionally inside the task loop.
  - A chain of tasks like `APP_TASKS=task1;task2` stopped after running only `task1`.
- **Change**:
  - Loop now continues across all tasks when tasks succeed, returning immediately only on failure with the 1-based index exit code.

---

## 17. Case-Insensitive Matching Inversion in `EnumerableExtensions.FirstElementType`
- **Severity**: Medium / Schema & Type Detection Defect
- **Description**:
  - `FirstElementType` had an inverted check in the fallback pass: `if (entry.Key.EqualsIgnoreCase(key)) continue;`, which skipped matching keys and returned the type of the first non-matching key.
- **Change**:
  - Corrected to `if (!entry.Key.EqualsIgnoreCase(key)) continue;`.

---

## 18. Thread Synchronization and Deadlock Prevention in `CommandsUtils`
- **Severity**: High / Concurrency & Race Condition
- **Description**:
  - `CommandResultsHandler<T>.Execute()` appended results to a shared `List<T>` concurrently across thread pool workers without locking.
  - If a command threw an exception, `waitHandle.Set()` was never invoked, causing the caller in `WaitAll` to hang until timeout.
- **Change**:
  - Synchronized `results.AddRange` with `lock (results)`.
  - Wrapped execution in `try ... finally { waitHandle.Set(); }` across `CommandResultsHandler`, `CommandExecsHandler`, and `ActionExecHandler`.
  - Disposed `WaitHandle` instances in `CommandsUtils.ExecuteAsyncCommandList` in a `finally` block.

---

## 19. Port Truncation in `SiteUtils.UrlFromSlug`
- **Severity**: Low / URL Parsing
- **Description**:
  - Slugs with ports without delimiters (e.g. `techstacks.io:8`) stripped the last digit via `atPort.Substring(0, atPort.Length - 1)`.
- **Change**:
  - Used `atPort` directly when no delimiters follow.

---

## 20. Empty Dictionary Sequence Crash in `Inspect.dumpInternal` & Type Mapping in `UseType`
- **Severity**: Low / Crash Prevention
- **Description**:
  - `Inspect.dumpInternal` calculated `obj.Keys.Map(x => x.Length).Max() + 2` without checking if the dictionary had any entries. Dumping an empty dictionary threw `InvalidOperationException: Sequence contains no elements`.
  - In `Inspect.UseType`, an empty dictionary implementing `IEnumerable` had no elements (`FirstOrDefault` returned null), erroneously falling through to `new List<object>(...)` and converting the dictionary to an empty list.
- **Change**:
  - Added empty count check returning `"{}"` immediately, and loop-based max key length calculation.
  - Added explicit `if (instance is IDictionary) return instance.ToObjectDictionary();` check before checking `IEnumerable`.

---

## 21. Operator Precedence Bug in `JSON.parseSpan`
- **Severity**: Medium / Security & Logic Bug
- **Description**:
  - In `JSON.cs`, `else if (firstChar == '{' || firstChar == '[' && !isEscapedJsonString(json.TrimStart()))` was evaluated as `firstChar == '{' || (firstChar == '[' && !isEscapedJsonString(json.TrimStart()))` because in C# `&&` binds tighter than `||`.
  - For JSON objects starting with `{`, the escape check was bypassed completely.
- **Change**:
  - Properly parenthesized the condition: `(firstChar == '{' || firstChar == '[') && !isEscapedJsonString(json.TrimStart())`.

---

## 22. Exception Argument Inversion in `UrnId.Parse` & Zero-Allocation Hardening
- **Severity**: Low / Robustness
- **Description**:
  - `UrnId.Parse` threw `new ArgumentException("Cannot parse invalid urn: '{0}'", urnId)`, where `urnId` was passed as `paramName` while the format placeholder was left unformatted.
  - Calling `urnId.Contains(FieldSeperator.ToString())` allocated unnecessary strings on every check.
- **Change**:
  - Replaced with string interpolation `$"Cannot parse invalid urn: '{urnId}'", nameof(urnId)`.
  - Switched separator check to zero-allocation char check `urnId.Contains(FieldSeperator)` and added null guards across `Create`, `CreateWithParts`, and generic `Create<T>`.

---

## 23. `PlatformNotSupportedException` in `ActionExecExtensions.ExecAllAndWait` on .NET Core
- **Severity**: Medium / Modernization & Crash Prevention
- **Description**:
  - `ActionExecExtensions.ExecAllAndWait` called `action.BeginInvoke(...)`, which throws `PlatformNotSupportedException` on .NET Core / .NET 5+.
  - `WaitHandle.WaitAll` has a maximum limit of 64 wait handles on Windows/BCL, throwing `NotSupportedException` if more than 64 actions were scheduled.
- **Change**:
  - Switched to `ExecAsync` on modern .NET with guaranteed `WaitHandle` disposal in `finally`.
  - Added chunked wait loop when handles exceed 64 to prevent `NotSupportedException`.

---

## 24. Thread Synchronization & Deadlock Inconsistency in `InMemoryLogFactory`
- **Severity**: Medium / Concurrency & Race Condition
- **Description**:
  - `InMemoryLogFactory` synchronized log writes using `lock (syncLock)` in some methods and `lock (this)` in others (such as `HasExceptions`). This inconsistent locking could lead to concurrent access races or deadlocks.
- **Change**:
  - Unified all synchronization on `syncLock`. Protected string formatting against `FormatException` for raw messages containing curly braces.

---

## 25. Concurrent Registration Race Condition in `StartupTasks`
- **Severity**: Medium / Concurrency & Race Condition
- **Description**:
  - `StartupTasks.Register` mutated `Tasks` dictionary without thread synchronization during application/plugin startup.
  - `StartupTasks.Run` enumerated `Tasks` directly while concurrent registrations could still take place, leading to `InvalidOperationException: Collection was modified`.
- **Change**:
  - Synchronized `Register` and snapshotted task actions in `Run` using `lock (Tasks)`.


