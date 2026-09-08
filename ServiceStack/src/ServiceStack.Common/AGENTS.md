# CONTEXT: ServiceStack.Common

## Purpose

`ServiceStack.Common` is the foundational shared utility library for the ServiceStack ecosystem. It provides a broad set of shared utility types, extension methods, virtual file system (VFS) abstractions, process execution utilities, scripting/template infrastructure, and cross-cutting concerns that are used throughout nearly every other ServiceStack package. It is not a server-side or client-side library exclusively — it is designed for both contexts and has no dependency on ServiceStack's HTTP pipeline.

**Target frameworks**: `net472;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

`ServiceStack.Common` sits near the base of the ServiceStack dependency graph:

- **Depends on**: `ServiceStack.Text` (serialization), `ServiceStack.Interfaces` (core abstractions/interfaces)
- **Depended on by**: `ServiceStack`, `ServiceStack.Server`, `ServiceStack.AI`, `ServiceStack.OrmLite`, and nearly all other packages

It is a **required transitive dependency** for almost every ServiceStack application. Changes here affect the entire ecosystem. Avoid introducing new heavy dependencies; the package must remain lightweight enough for client-side use.

---

## Key Functionality

### Virtual File System (VFS)
A unified file abstraction layer over physical file systems, in-memory stores, and CDN-backed content.

| Type | Role |
|---|---|
| `FileSystemVirtualFiles` | Physical file system provider; root for path safety checks |
| `AbstractVirtualFileBase` | Base class for virtual files; provides default `ReadAllBytes` via `OpenRead` |
| `AbstractVirtualPathProviderBase` | Base for path providers; resolves combined paths |
| `MultiVirtualFiles` | Aggregates multiple `IVirtualPathProvider` instances with waterfall resolution |
| `FileSystemVirtualDirectory` | Represents a physical directory; enumerates files and subdirectories |
| `MultiVirtualDirectory` | Aggregates directories across multiple providers |
| `FileSystemMapping` | Maps a virtual path prefix to a physical directory |

**Critical**: `FileSystemVirtualFiles.IsPathSafe` must validate that resolved paths fall strictly _inside_ the base directory (directory-separator boundary check, not just `StartsWith`). See Security section.

### Process Execution
| Type | Role |
|---|---|
| `ProcessUtils` | Async process spawning with stdout/stderr capture, timeout, and cancellation |
| `ProcessUtils.RunAsync` | Primary async entry point; supports `CancellationToken` and timeouts |
| `ProcessUtils.FindExePath` | Locates an executable on `PATH` |
| `ExecUtils` | Multi-threaded command execution patterns |
| `ExecUtils.ExecAllWithFirstOut` | Runs commands in parallel, returns the first successful result |
| `ExecUtils.ExecAllAndWait` | Runs commands in parallel and waits for all to complete |

### Command Execution & Thread Management
| Type | Role |
|---|---|
| `CommandsUtils` | Thread-safe parallel command execution with `WaitHandle` management |
| `CommandResultsHandler` | Collects results from parallel commands; must use `lock (results)` |
| `CommandExecsHandler` | Executes commands across threads, signals completion via `WaitHandle` |

### Script / Template Engine
| Type | Role |
|---|---|
| `ProtectedScripts` | Sandboxed script methods for server-side template evaluation |
| `ScriptContext` | Configures and executes `#Script` templates |
| View utilities | Helpers for rendering views within the template engine pipeline |

### String Utilities (`StringUtils`)
- `ReplaceOutsideOfQuotes` — replaces tokens in strings while respecting single/double-quoted regions (must skip `\"` and `\'` escaped quotes)
- `ParseTypeIntoNodes` — parses generic type signatures into a node tree
- `SnakeCaseToPascalCase` — naming convention conversion
- `SplitGenericArgs` — splits generic type argument lists
- SQL/command expression parsing helpers

### View Utilities (`ViewUtils`)
- HTML navigation helpers, `NavLink` rendering
- `TextDumpOptions`, `HtmlDumpOptions` — structured dump formatting
- `ToKeyValues` — converts objects/dictionaries to key-value pairs for template rendering

### Logging
| Type | Role |
|---|---|
| `InMemoryLogFactory` | `ILogFactory` implementation backed by in-memory storage; used in tests |
| `InMemoryLog` | Thread-safe in-memory log that stores `LogEntry` records per level |

### IoC / Settings
| Type | Role |
|---|---|
| `SimpleContainer` | Lightweight IoC container for apps without a ServiceStack host |
| `SimpleAppSettings` | Thread-safe key/value settings dictionary |
| `StartupTasks` | Thread-safe registration and execution of startup actions |

### Database Profiling
| Type | Role |
|---|---|
| `ProfiledDbDataReader` | MiniProfiler-aware `IDataReader` wrapper |
| `ProfiledCommand` | MiniProfiler-aware `IDbCommand` wrapper |
| `ProfiledConnection` | MiniProfiler-aware `IDbConnection` wrapper |

### Miscellaneous Utilities
| Type | Role |
|---|---|
| `UrnId` | URN-format ID generation and parsing (`urn:type:id`) |
| `FuncUtils` | `TryExec<T>` functional error-suppression helpers |
| `AppTasks` | Chained task execution for CLI/startup app tasks |
| `GitHubGateway` | GitHub API client focused on Gist operations |
| `SvgCreator` | SVG markup generation utilities |
| `VirtualPathUtils` | File path normalization, retry/backoff helpers |
| `TypeExtensions` | Reflection utilities, compiled property accessor expressions |
| `XLinqExtensions` | XML/LINQ extension methods |
| `JSON` | Custom lightweight JSON string parsing (complement to `ServiceStack.Text`) |

---

## Architecture & Design Patterns

### Layered Abstraction (VFS)
The VFS follows a classic provider pattern with a chain of fallback resolution:

```
MultiVirtualFiles
  └─► FileSystemVirtualFiles   (physical disk)
  └─► MemoryVirtualFiles        (in-memory, e.g. embedded resources)
  └─► (custom providers)
```

Each provider implements `IVirtualPathProvider`. Directories implement `IVirtualDirectory`. Files implement `IVirtualFile`. Base classes (`AbstractVirtualFileBase`, `AbstractVirtualPathProviderBase`) provide default behaviour that concrete classes can override selectively.

### Thread-Safety Conventions
All shared mutable state must be guarded:
- `CommandResultsHandler` — `lock (results)` around list mutations
- `SimpleAppSettings` — `lock (settings)` around all reads and writes; `GetAll()` returns a **copy**
- `InMemoryLog` — lock around every log append and read
- `StartupTasks` — lock around registration and single-run guarantee

### Backoff / Retry
`VirtualPathUtils.SleepBackOffMultiplier` computes exponential back-off delays. Uses bit-shift (`1 << Math.Min(i, 10)`) rather than `Math.Pow` to avoid floating-point issues, and caps the shift at 10 to prevent overflow.

### WaitHandle-Based Parallel Execution
`CommandsUtils` uses `WaitHandle.WaitAll` with a `ManualResetEvent` per command. Each command handler **must** call `waitHandle.Set()` in a `finally` block to prevent permanent deadlock if the command throws.

### Functional Helpers
`FuncUtils.TryExec<T>` follows the Try-pattern (returns `default` on exception) for use in contexts where exceptions must not propagate (e.g., template rendering, diagnostics).

---

## Security & Reliability Considerations

> These represent past vulnerabilities that have been fixed. Reviewers should ensure changes in related areas do not re-introduce them.

### Path Traversal — `FileSystemVirtualFiles.IsPathSafe` (HIGH)
**Risk**: An attacker-controlled path like `../app_secret/file` could satisfy a naive `StartsWith(basePath)` check while actually escaping the base directory.  
**Fix**: The resolved absolute path must start with `basePath + Path.DirectorySeparatorChar` (or equal `basePath` exactly), not just `basePath`. Never relax this check when modifying path resolution code.

### Logic Bug — `ExecAllWithFirstOut` (HIGH)
**Risk**: The original condition was inverted so `firstResult` was **never** assigned — the method always returned `null`/default regardless of successful execution.  
**Fix**: Use a dedicated `bool hasResult` flag to track whether any command has produced output, separate from null-checks on the result value.

### Deadlock — `CommandResultsHandler` / `CommandExecsHandler` (HIGH)
**Risk**: If a command handler throws before calling `waitHandle.Set()`, the calling thread blocks on `WaitHandle.WaitAll` forever.  
**Fix**: `waitHandle.Set()` must always be called in a `finally` block. The results list must be accessed under `lock (results)` to prevent data races.

### StackOverflow — `AbstractVirtualFileBase.ReadAllBytes`
**Risk**: The default implementation called `ReadAllBytes()` recursively (infinite loop) instead of reading from `OpenRead()`.  
**Fix**: Default implementation must delegate to `OpenRead()` and read the stream, not call itself.

### InvalidCastException — `MultiVirtualDirectory.ParentDirectory`
**Risk**: Using `SelectMany` on child-directory enumerations returned grandchildren, causing an invalid cast when assigned to `IVirtualDirectory`.  
**Fix**: Use `Select` (not `SelectMany`) to project only immediate parents.

### Backoff Overflow — `SleepBackOffMultiplier`
**Risk**: `^` in C# is bitwise XOR, not exponentiation. Without capping the shift, large iteration counts overflow `int`.  
**Fix**: Use `1 << Math.Min(i, 10) * 50`. Cap the exponent at 10.

### Empty Dictionary Crash — `Inspect.dumpInternal`
**Risk**: Calling `.Max()` on an empty key collection throws `InvalidOperationException`.  
**Fix**: Check `obj.Keys.Count > 0` before calling `.Max()`.

### Operator Precedence — JSON `{` Condition
**Risk**: Without explicit parentheses around the leading `{` check, C# operator precedence can bypass an escape-character guard, allowing malformed JSON to be mis-parsed.  
**Fix**: Always use parentheses: `(c == '{' && !escaped)`.

### Quote Escaping — `ReplaceOutsideOfQuotes`
**Risk**: Escaped quotes (`\"`, `\'`) inside a string literal were being treated as quote delimiters, causing the "inside/outside" tracking to desynchronize.  
**Fix**: When iterating characters, detect and skip over `\"` and `\'` escape sequences before toggling the in-quote state.

### SimpleContainer — `AddSingleton` / `Dispose`
**Risk 1**: `AddSingleton(factory)` must store a **lazy** wrapper (`_ => factory()`) so the factory is called only once.  
**Risk 2**: `Dispose` must snapshot the container contents before clearing, then dispose the snapshot — otherwise the iteration and the clear race.

### SimpleAppSettings — Thread Safety
**Risk**: Unsynchronized reads alongside writes can produce torn reads or stale data.  
**Fix**: All methods (`Get`, `Set`, `GetAll`, `Remove`) must acquire `lock (settings)`. `GetAll()` must return a **new dictionary copy**, not a reference to the internal store.

---

## Common Modification Scenarios

1. **Adding a new VFS provider** — Subclass `AbstractVirtualPathProviderBase` and `AbstractVirtualFileBase`. Override `IsPathSafe` if the provider has its own path restrictions. Register in `MultiVirtualFiles`.

2. **Adding new script methods** — Add to `ProtectedScripts` or create a new `ScriptMethods` subclass and register it on `ScriptContext`. Follow sandbox restrictions — never expose methods that access the raw file system without VFS path safety checks.

3. **Adding new process/command utilities** — Follow the `WaitHandle` + `finally` pattern used in `CommandsUtils`. Always use `CancellationToken` overloads in `ProcessUtils`.

4. **Adding string/path utilities** — Add to `StringUtils` or `VirtualPathUtils`. Ensure new string parsers correctly handle escaped characters (see `ReplaceOutsideOfQuotes` fix).

5. **Adding settings or IoC helpers** — All new methods in `SimpleAppSettings` or `SimpleContainer` must acquire the appropriate lock. Singleton registrations must use lazy initialization.

6. **Adding new extension methods** — Target `TypeExtensions`, `XLinqExtensions`, or a purpose-specific `*Extensions` file. Prefer expression-compiled property accessors over raw reflection for performance-sensitive paths.

7. **Updating framework targets** — The project multi-targets `net472;net6.0;net8.0;net10.0`. Use `#if NET6_0_OR_GREATER` (or `#if NETFRAMEWORK`) guards when APIs differ. Avoid APIs unavailable on `net472` without a guard.
