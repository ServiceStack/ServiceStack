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

---

## 26. Thread-Safe Idempotent DbDataReader Close & Async Modernization in `ProfiledDbDataReader` & MiniProfiler
- **Severity**: Medium / Double Free & Resource Lifecycle
- **Description**:
  - `ProfiledDbDataReader` executed `profiler.ReaderFinish(this)` and `reader.Close()` non-idempotently. When callers invoked both `.Close()` and `.Dispose()`, reader callbacks fired twice, skewing profiling timings and state.
  - Async overrides (`ExecuteNonQueryAsync`, `ExecuteScalarAsync`, `ExecuteDbDataReaderAsync`, `ReadAsync`, `OpenAsync`, etc.) delegated to synchronous base DbCommand/DbConnection fallbacks rather than underlying wrapped ADO.NET connection/command async methods.
- **Change**:
  - Used `Interlocked.Exchange` to guarantee atomic single execution of `ReaderFinish` and `Close`.
  - Implemented async overrides in `ProfiledCommand`, `ProfiledConnection`, `ProfiledDbDataReader`, and `ProfiledDbTransaction` with profiling telemetry hooks and modern `IAsyncDisposable`.

---

## 27. Value-Type Expression Tree Boxing Crash & Circular Recursion Bug in `TypeExtensions`
- **Severity**: High / Expression Execution Failure & Stack Overflow
- **Description**:
  - `TypeExtensions.AddReferencedTypes` contained a recursive call `AddReferencedTypes(type, refTypes)` on the parent type itself instead of property type `p.PropertyType`, leading to self-referential infinite loops.
  - `TypeExtensions.CreatePropertyAccessorExpression` failed to box value-type property access expressions (`int`, `DateTime`, enums, structs) when compiling `Func<object, object>` lambdas, causing runtime `ArgumentException: Expression of type 'X' cannot be used for return type 'System.Object'`.
- **Change**:
  - Corrected recursion to inspect `p.PropertyType` and prevent circular loops.
  - Added boxing conversion `Expression.Convert(propExpr, typeof(object))` for value types in property accessor expression compilation.
  - Added defensive null checks across all reflector invoker and accessor helpers.

---

## 28. Malformed JSON Serialization & Out-of-Bounds Substring Crash in `GitHubGateway`
- **Severity**: Medium / API Malformed Payloads & Crash
- **Description**:
  - `GitHubGateway.WriteGistFiles` generated malformed JSON `{"files":{}"description":"..."}` missing comma separator when updating gists with description changes and no file changes.
  - `GistLink.Parse` performed unchecked substring `url.Substring("https://".Length)` without verifying length or protocol scheme, throwing `ArgumentOutOfRangeException` on short or non-HTTPS URLs.
  - `GistLink.Parse` inadvertently treated backtick-delimited tags on links without `{}` blocks as JS template tokens.
- **Change**:
  - Ensured valid JSON separator formatting in `WriteGistFiles`.
  - Hardened URL scheme handling and backtick modifier parsing in `GistLink.Parse`.
  - Added defensive null guards in collection iteration and pagination loops.

---

## 29. BCL Compatibility & Task Cancellation Optimization in `UrnId` & `AsyncManualResetEvent`
- **Severity**: Low / Framework Compatibility & Performance
- **Description**:
  - `UrnId` used .NET Core-only `string.Contains(char)`, causing compilation and runtime incompatibilities on .NET Framework 4.7.2.
  - `TaskExtensions.WaitAsync` allocated manual TaskCompletionSource instances instead of leveraging BCL native `Task.WaitAsync` on .NET 6+.
- **Change**:
  - Replaced `string.Contains(char)` with universal `string.IndexOf(char) >= 0`.
  - Routed `TaskExtensions.WaitAsync` to native BCL `Task.WaitAsync` on modern .NET targets.

---

## 30. Unhandled Bracket Stack Underflow, Index Exceptions & Quote Escape Bypasses in `StringUtils`
- **Severity**: Medium / Parsing Exception & Security Filter Bypass
- **Description**:
  - `StringUtils.ReplaceOutsideOfQuotes` treated backslash characters verbatim without skipping escaped quotes (`\"`, `\'`), leading to corrupted state machine tracking where strings with escaped quotation marks caused code inside quotes to be treated as outside of quotes and vice versa.
  - `StringUtils.ParseTypeIntoNodes` threw `InvalidOperationException: Stack empty` on malformed type definitions with unmatched closing generic brackets (`>`), crashing type inspection.
  - `StringUtils.SnakeCaseToPascalCase` threw `IndexOutOfRangeException` when passed strings consisting solely of invalid/stripped characters.
  - `StringUtils.SplitGenericArgs` corrupted block counting on unmatched closing brackets (`>`).
- **Change**:
  - Added escaped quote tracking (`\"`, `\'`, `\``, `\′`) in `ReplaceOutsideOfQuotes` and null safety.
  - Guarded `blockStartingPos.Pop()` against empty stacks in `ParseTypeIntoNodes`.
  - Added empty string check before indexing in `SnakeCaseToPascalCase` and optimized with `StringBuilderCache`.
  - Clamped `blockCount` to zero in `SplitGenericArgs`.

---

## 31. Malformed HTML Output, Unchecked Cast Exceptions & Thread Safety in `ViewUtils`
- **Severity**: Medium / HTML Malformation, Unhandled Exception & Race Condition
- **Description**:
  - `ViewUtils.NavLink` generated malformed HTML `</div` (missing closing `>`) and non-standard `</lI>`.
  - `ViewUtils.ActiveClass` threw `NullReferenceException` when `ActivePath` was null (the default initialization).
  - `ViewUtils.ToKeyValues` threw `InvalidCastException` when converting `IEnumerable<object>` containing non-string primitives (integers, booleans, enums) due to direct LINQ type filtering cast (`from string item in list`).
  - `TextDumpOptions.Parse` and `HtmlDumpOptions.Parse` threw `NullReferenceException` when `options` dictionary was null, and threw unhandled cast exceptions when option values were not raw strings.
  - `ViewUtils.Load` and `GetNavItems` accessed and initialized static collections (`NavItems`, `NavItemsMap`) without thread synchronization, creating race conditions in concurrent web requests.
- **Change**:
  - Corrected `</div` to `</div>` and normalized `</li>` in `NavLink`.
  - Added null guard in `ActiveClass` returning empty string when `activePath` is null.
  - Replaced restrictive LINQ cast in `ToKeyValues` with universal `item.AsString()` projection.
  - Hardened `TextDumpOptions.Parse` and `HtmlDumpOptions.Parse` against null dictionaries and safe string conversion.
  - Added lock synchronization to static navigation collection loaders in `ViewUtils`.

---

## 32. Truncation and Parsing Breakage on Quoted Aliases & Identifiers in `Command`
- **Severity**: Low / Query Syntax Truncation
- **Description**:
  - `Command.IndexOfMethodEnd` truncated identifier parsing at underscore (`_`) and dollar sign (`$`), corrupting SQL/command parsing for aliases containing underscores (e.g. `SUM(*) as total_count`).
  - It failed to handle quoted aliases (`"alias"`, `'alias'`, `` `alias` ``, `[alias]`) and flexible whitespace between `AS` and the alias.
- **Change**:
  - Extended identifier character support to alphanumeric, `_`, and `$`.
  - Added support for quoted alias syntax across common SQL quote delimiters.

---

## 33. Race Conditions & State Leakage in `SimpleAppSettings`
- **Severity**: Low / Concurrency Safety & Mutability Leak
- **Description**:
  - `SimpleAppSettings` performed read, write, and collection iteration over its internal dictionary without synchronization, risking `InvalidOperationException: Collection was modified` under concurrent access.
  - `GetAll()` returned a direct reference to the internal mutable dictionary, allowing external callers to unintentionally mutate application configuration.
- **Change**:
  - Synchronized all read and write dictionary operations with `lock (settings)`.
  - Returned a defensive snapshot copy (`new Dictionary<string, string>(settings)`) from `GetAll()`.

---

## 34. Integer Overflow on `int.MinValue` & Missing Color Guards in `SvgCreator`
- **Severity**: Low / Runtime Arithmetic Overflow
- **Description**:
  - `SvgCreator.GetDarkColor` executed `Math.Abs(index) % colors.Length`. In .NET, `Math.Abs(int.MinValue)` throws `OverflowException` because `int.MinValue` has no positive 32-bit two's-complement representation.
  - `CreateSvg` did not guard against null or empty `DarkColors` collections.
- **Change**:
  - Replaced `Math.Abs` with bitwise masking `(index & 0x7FFFFFFF) % colors.Length` to eliminate overflow risk.
  - Added null safety checks for `DarkColors` and null-propagation in `ToDataUri`.
