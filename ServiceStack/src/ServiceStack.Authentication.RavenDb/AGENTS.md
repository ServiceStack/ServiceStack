# CONTEXT: ServiceStack.Authentication.RavenDb

## Purpose

`ServiceStack.Authentication.RavenDb` provides a RavenDB-backed implementation of ServiceStack's user authentication repository interfaces. It persists user accounts, OAuth provider links, and API key records as RavenDB documents, enabling ServiceStack applications that use RavenDB as their primary database to avoid a secondary SQL/OrmLite store for authentication data.

The library is a direct alternative to `ServiceStack.Authentication.MongoDb` and the built-in OrmLite-based auth repository. It slots in via ServiceStack's `IUserAuthRepository` / `IUserAuthRepositoryAsync` contracts, which the ServiceStack auth pipeline calls at runtime — meaning the host application only needs to register `RavenDbUserAuthRepository` in the IoC container and no other auth code changes are required.

---

## Role in ServiceStack Ecosystem

### Dependencies
| Dependency | Role |
|---|---|
| `ServiceStack` (core) | Provides `IUserAuthRepository`, `IUserAuthRepositoryAsync`, `IUserAuth`, `IUserAuthDetails`, `IApiKeySource`, `AuthSession`, `IRequest` and all auth pipeline abstractions |
| `RavenDB.Client` | Document session, `IDocumentStore`, `IAsyncDocumentSession`, and index infrastructure |

### Consumers
Any ServiceStack application that:
1. Registers `IDocumentStore` in its IoC container, and
2. Registers `RavenDbUserAuthRepository` as `IUserAuthRepository`

will use this library for all credential, OAuth provider, and API key persistence.

### Peer projects
- `ServiceStack.Authentication.MongoDb` — functionally equivalent, targets MongoDB
- `ServiceStack.Auth` (OrmLite) — SQL-backed equivalent, built into `ServiceStack` core
- `ServiceStack.Authentication.LiteDB` — LiteDB-backed alternative

---

## Key Functionality

### `RavenDbUserAuthRepository`
The central class. Generic over `TUserAuth : class, IUserAuth` and `TUserAuthDetails : class, IUserAuthDetails`, defaulting to the concrete `UserAuth` / `UserAuthDetails` types shipped in ServiceStack core.

Implements:
- `IUserAuthRepository` (synchronous) — full CRUD for user accounts and provider details
- `IUserAuthRepositoryAsync` — async counterparts of every synchronous method, propagating `CancellationToken` through all RavenDB session calls with `.ConfigAwait()`
- `IClearable` — `Clear()` purges all `TUserAuth` and `TUserAuthDetails` documents (used in tests)
- `IManageApiKeys` / `IManageApiKeysAsync` — stores, retrieves, and invalidates `ApiKey` documents in RavenDB

Key methods:
| Method | Notes |
|---|---|
| `CreateUserAuth` / `CreateUserAuthAsync` | Validates credentials, hashes passwords, persists new user document |
| `UpdateUserAuth` / `UpdateUserAuthAsync` | Re-validates and updates existing user document |
| `GetUserAuthByCredentials` | Loads by username/email, verifies password hash |
| `SaveUserAuthDetails` / `SaveUserAuthDetailsAsync` | Upserts an `IUserAuthDetails` OAuth provider record and back-links to the user |
| `GetUserAuthDetails` | Queries `UserAuth_By_UserAuthDetails` index to find all providers for a user |
| `DeleteUserAuth` / `DeleteUserAuthAsync` | Cascade-deletes the user doc and all related `TUserAuthDetails` docs — **must null-check `userAuth` before calling `session.Delete`** |
| `LoadUserAuth` / `SaveUserAuth` | Session-linked load/save wiring expected by the auth pipeline |
| `GetApiKeyUser` | Resolves a `UserAuth` from an API key string |

### `RavenIdConverter`
Utility class responsible for translating between the two ID spaces:

- **ServiceStack ID model**: integer (e.g. `1`)
- **RavenDB document key**: composite string (e.g. `RavenUserAuths/1-A`)

Critical methods:
- `ToRavenId(int id)` — formats an integer as a RavenDB key
- `ToInt(string ravenId)` — parses a RavenDB key back to an integer; **must use `TryToInt` internally** — throws are not acceptable on malformed or unexpected input
- `TryToInt(string ravenId, out int id)` — safe variant that returns `false` on failure; handles plain integers, single-segment keys, deep-path keys, and any other non-standard format without throwing

All callers that need an integer user ID from a string should prefer `TryToInt`.

### `UserAuth_By_UserAuthDetails` (static index)
A RavenDB `AbstractIndexCreationTask` that indexes `TUserAuthDetails` documents by their `UserAuthId` field. Used by `GetUserAuthDetails` to efficiently retrieve all OAuth provider records associated with a given user without a full collection scan.

Index creation is performed once at startup via `EnsureThatUniqueIndexesAreCreated`.

### `RegisterPopulator`
Registers a custom AutoMapper populator that copies base `UserAuth` fields into `TUserAuth` instances. Uses a **static boolean flag with double-checked locking** to ensure the populator is registered exactly once regardless of how many `RavenDbUserAuthRepository` instances are created. Without this guard every new instance wraps the previous delegate chain, causing a memory/stack-depth leak.

### `EnsureThatUniqueIndexesAreCreated`
Ensures the `UserAuth_By_UserAuthDetails` index is deployed to RavenDB exactly once per process lifetime. Guarded by `lock (initLock)` + a static `bool` flag. Without the lock this is vulnerable to a race condition during startup with multiple concurrent requests.

---

## Architecture & Design Patterns

### Generic document model
`RavenDbUserAuthRepository<TUserAuth, TUserAuthDetails>` is fully generic, allowing host apps to extend the base `UserAuth` type with custom properties while keeping the same repository logic. The non-generic `RavenDbUserAuthRepository` class is a concrete alias with `UserAuth` / `UserAuthDetails`.

### Synchronous/Asynchronous symmetry
Every public operation exists in both a synchronous form (opens a `IDocumentSession`) and an asynchronous form (opens an `IAsyncDocumentSession`). Async methods accept and propagate a `CancellationToken` through every awaited call. All `await` expressions use `.ConfigAwait()` (ServiceStack's `ConfigureAwait(false)` wrapper) to avoid deadlocks in synchronisation-context-heavy hosts.

### Session-per-operation
Each repository method opens and disposes its own RavenDB session within a `using` block. There is no ambient/shared session held on the repository instance, which keeps the class thread-safe and avoids stale-state bugs.

### ID translation boundary
All conversions between string Raven keys and integer ServiceStack user IDs are centralised in `RavenIdConverter`. No caller should perform ad-hoc `int.Parse` or string formatting — route everything through this class.

### Once-only initialisation
Both `RegisterPopulator` and `EnsureThatUniqueIndexesAreCreated` follow the standard double-checked locking pattern for one-time initialization:

```csharp
if (!initialized)          // fast path (no lock)
{
    lock (initLock)
    {
        if (!initialized)  // re-check inside lock
        {
            // ... do work ...
            initialized = true;
        }
    }
}
```

---

## Security & Reliability Considerations

### ID parsing — never use `int.Parse` directly
`authSession.UserAuthId` arriving from a live session is already a full RavenDB key string (`RavenUserAuths/1-A`), **not** a plain integer. Calling `int.Parse` on it throws `FormatException` and crashes session saves. Always use `RavenIdConverter.TryToInt` and handle the failure case gracefully.

Inputs that `TryToInt` must survive without throwing:
- Plain integers: `"1"`
- Standard Raven keys: `"RavenUserAuths/1-A"`
- Single-segment keys (no `/`): `"foo"`
- Deep paths: `"a/b/c/d"`
- `null` / empty string

### Cascade delete — null-check before delete
`DeleteUserAuth` loads the user document first; if it does not exist, the variable is `null`. Calling `session.Delete(null)` throws. Always guard:

```csharp
if (userAuth != null)
    session.Delete(userAuth);
```

Related `TUserAuthDetails` documents must be retrieved via `.OfType<TUserAuthDetails>()` on the session's tracked entities, not cast blindly, to avoid `InvalidCastException`.

### Populator memory leak — static flag required
Registering the AutoMapper populator inside the constructor without a static guard causes each new repository instance to wrap the previous delegate. In IoC containers that create the repository more than once (e.g. transient scope, test teardown/setup cycles) this produces unbounded delegate chain growth. The static double-checked-lock pattern is mandatory.

### Race condition on index creation
`EnsureThatUniqueIndexesAreCreated` touches shared mutable state (`static bool`). Without `lock (initLock)` two concurrent requests during startup can both observe `initialized == false` and both attempt to create the index simultaneously, which in some RavenDB versions can result in an error or duplicate work. The lock is required.

### Digest authentication — safe header access
When inspecting request headers for Digest auth, use `TryGetValue` rather than the direct dictionary indexer. Missing headers must be treated as absent, not as an exception.

### Async cancellation
All `IAsyncDocumentSession` operations accept a `CancellationToken`. Pass the token received from the caller through every `.LoadAsync(...)`, `.StoreAsync(...)`, `.SaveChangesAsync(...)`, and `.ToListAsync(...)` call. Omitting the token prevents cooperative cancellation under load-shedding or request timeout scenarios.

---

## Common Modification Scenarios

### Adding a new field to user auth
Extend `UserAuth` (or a custom `TUserAuth` subclass) with the new property. RavenDB's schema-less document model means no migration is needed for new optional fields. If the field is required for queries, add a new static index class and register it in `EnsureThatUniqueIndexesAreCreated`.

### Adding a new query method
1. Open an `IDocumentSession` (sync) or `IAsyncDocumentSession` (async) from the injected `IDocumentStore`.
2. Use LINQ or `session.Query<T, IndexClass>()` for indexed queries.
3. Use `RavenIdConverter.TryToInt` / `ToRavenId` whenever converting between ID representations.
4. Dispose the session with `using`.
5. Add the async variant with `CancellationToken` and `.ConfigAwait()`.

### Extending to a new `IUserAuthRepository` interface method
ServiceStack occasionally adds new methods to auth repository interfaces. Implement both the sync and async overload, following the session-per-operation pattern, and use `TryToInt` for any ID parsing. Add unit tests that exercise malformed ID inputs.

### Changing the RavenDB key prefix
`RavenIdConverter` contains the `"RavenUserAuths/"` prefix constant. Update it there and nowhere else. Ensure existing data migration is handled if changing a production database's key scheme.

### Supporting additional target frameworks
The project targets `net472;net6.0;net8.0;net10.0`. Any new conditional compilation (`#if NET6_0_OR_GREATER`, etc.) should be confined to the smallest possible scope, and the `net472` path must remain functional for legacy consumers.

---

## Target Frameworks

| TFM | Notes |
|---|---|
| `net472` | Legacy .NET Framework support |
| `net6.0` | LTS baseline |
| `net8.0` | Current LTS |
| `net10.0` | Latest |
