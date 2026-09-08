# CONTEXT: ServiceStack.Authentication.MongoDb

## Purpose

`ServiceStack.Authentication.MongoDb` implements ServiceStack's user authentication repository interfaces using MongoDB as the backing store. It enables ServiceStack applications to persist user accounts, linked OAuth provider associations, and API keys in a MongoDB database rather than a relational database or in-memory store.

This package is the MongoDB-specific adapter that bridges ServiceStack's auth system (which operates against abstract interfaces) and the MongoDB.Driver client library. It is a drop-in alternative to other repository providers such as OrmLite (`ServiceStack.Server`) or RavenDb.

**Target Frameworks**: `net472`, `net6.0`, `net8.0`, `net10.0`

---

## Role in ServiceStack Ecosystem

### Depends On
- **`ServiceStack` core** — Provides the auth interfaces (`IUserAuthRepository`, `IUserAuthRepositoryAsync`, `IManageApiKeys`, `IManageApiKeysAsync`), domain types (`IUserAuth`, `IUserAuthDetails`, `ApiKey`), and auth feature infrastructure.
- **`MongoDB.Driver`** — The official MongoDB .NET client used for all database interactions.

### Used By
- Any ServiceStack application that configures `MongoDbAuthRepository` as its `IUserAuthRepository` plugin registration. The repository is typically registered in `AppHost.Configure()` via `container.Register<IUserAuthRepository>(...)`.

### Sibling Providers
Other auth repository implementations in the ecosystem include:
- `ServiceStack.Server` — OrmLite (RDBMS-backed) auth repository
- `ServiceStack.Authentication.RavenDb` — RavenDB-backed repository
- In-memory repository (built into ServiceStack core, for testing/development)

All share the same `IUserAuthRepository` / `IUserAuthRepositoryAsync` contracts, making them interchangeable at the registration site.

---

## Key Functionality

### Primary Classes

| Class | Description |
|---|---|
| `MongoDbAuthRepository` | Synchronous implementation of `IUserAuthRepository`, `IManageApiKeys`. Handles CRUD for user accounts and OAuth provider links. |
| `MongoDbAuthRepositoryAsync` | Async counterpart implementing `IUserAuthRepositoryAsync` and `IManageApiKeysAsync`. Should use fully async MongoDB driver calls throughout (no sync-over-async). |

### MongoDB Collections

| Collection | Maps To | Purpose |
|---|---|---|
| `UserAuth` | `IUserAuth` / `UserAuth` | Primary user account documents (credentials, roles, permissions, metadata). |
| `UserAuthDetails` | `IUserAuthDetails` / `UserAuthDetails` | Per-OAuth-provider linkage records (provider name, access token, user info). |
| `ApiKey` | `ApiKey` | Issued API keys linked to user accounts. |

### Key Methods

- **`CreateUserAuth` / `CreateUserAuthAsync`** — Validates uniqueness, hashes the password, and inserts a new `UserAuth` document.
- **`UpdateUserAuth` / `UpdateUserAuthAsync`** — Updates an existing `UserAuth` document. Must pass the pre-update snapshot (`existingUser`) to uniqueness checks.
- **`GetUserAuthByUserName` / `GetUserAuthByUserNameAsync`** — Resolves a user by username **or** email (detects `@` to decide which field to query).
- **`GetUserAuth` / `GetUserAuthAsync`** — Resolves a user by their integer ID.
- **`DeleteUserAuth` / `DeleteUserAuthAsync`** — Removes a `UserAuth` document **and** all associated `UserAuthDetails` documents for that user.
- **`GetUserAuthDetails` / `GetUserAuthDetailsAsync`** — Returns all OAuth provider links for a given user ID.
- **`CreateOrMergeAuthSession` / `CreateOrMergeAuthSessionAsync`** — Upserts a `UserAuthDetails` record from an active OAuth session.
- **`AssertNoExistingUser` / `AssertNoExistingUserAsync`** — Enforces username and email uniqueness by querying MongoDB directly. Excludes the `existingUser` from conflict detection during updates.
- **`CollectionsExists` / `CollectionsExistsAsync`** — Guards entry points by verifying all required collections are present in the database.
- **`GetApiKeys` / `StoreAll` (ApiKey)** — Implements `IManageApiKeys` / `IManageApiKeysAsync` for API key issuance and storage.

---

## Architecture & Design Patterns

### Dual Sync/Async Surface
The repository exposes two classes: one fully synchronous (`MongoDbAuthRepository`) and one fully asynchronous (`MongoDbAuthRepositoryAsync`). Both wrap the same MongoDB collections. The async class must use `FindAsync`, `InsertOneAsync`, `ReplaceOneAsync`, `DeleteManyAsync`, etc. throughout — it must **not** call `.Result` or `.GetAwaiter().GetResult()` on async MongoDB driver calls, as this causes thread pool starvation under concurrent load.

### Collection Initialization Guard
Before any operation is performed, the implementation verifies that the required MongoDB collections (`UserAuth`, `UserAuthDetails`, `ApiKey`) exist. This fail-fast pattern surfaces misconfiguration early rather than producing cryptic MongoDB errors at runtime.

### Uniqueness Enforcement
Username and email uniqueness is enforced in application code (not via MongoDB unique indexes alone) via `AssertNoExistingUser`. This method issues two **separate** MongoDB queries — one against the `UserName` field and one against the `Email` field. It does **not** reuse `GetUserAuthByUserName` for this check, because that helper conflates usernames containing `@` with email addresses, which can produce false negatives.

### ID Handling
User IDs are stored as integers in `UserAuth`. When resolving a user by session-provided string IDs, the code must use `int.TryParse` — not `int.Parse` — so that malformed or missing IDs return `null` gracefully without throwing an exception.

### Digest Authentication
When looking up users for digest auth challenges, header dictionary access must use `TryGetValue` rather than the direct indexer (`[]`). The indexer throws `KeyNotFoundException` when a header is absent; `TryGetValue` returns `false` safely.

---

## Security & Reliability Considerations

> These represent historically confirmed bug patterns in this codebase. Pay close attention when modifying any of the listed methods.

### 1. `UpdateUserAuth` — Pass `existingUser` to Uniqueness Check
`AssertNoExistingUser` must receive the **current** user snapshot as `existingUser` so it can exclude that user's own username and email from the conflict query. If `null` is passed instead, every update to a user who already has a username or email will incorrectly throw `"User already exists"`.

```csharp
// CORRECT
AssertNoExistingUser(newUser, existingUser);

// BUG — treats own username/email as a conflict
AssertNoExistingUser(newUser, null);
```

### 2. Async Methods — No Sync-Over-Async
All methods in `MongoDbAuthRepositoryAsync` must use the async MongoDB driver APIs end-to-end:

```csharp
// CORRECT
var user = await collection.Find(filter).FirstOrDefaultAsync();

// BUG — blocks a thread pool thread, causes starvation under load
var user = collection.Find(filter).FirstOrDefault();
```

### 3. `DeleteUserAuth` — Use `DeleteMany` for `UserAuthDetails`
A user may have multiple `UserAuthDetails` records (one per linked OAuth provider). Deleting only one with `DeleteOne` leaves orphaned records in MongoDB:

```csharp
// CORRECT
userAuthDetails.DeleteMany(x => x.UserAuthId == userAuthId);

// BUG — leaves orphaned OAuth provider links
userAuthDetails.DeleteOne(x => x.UserAuthId == userAuthId);
```

### 4. ID Parsing — Use `int.TryParse`
Session `UserAuthId` values arrive as strings and may be empty, null, or non-numeric. Always use `TryParse`:

```csharp
// CORRECT
if (!int.TryParse(userAuthId, out var intId)) return null;

// BUG — throws FormatException on invalid IDs
var intId = int.Parse(userAuthId);
```

### 5. Digest Auth Header — Use `TryGetValue`
```csharp
// CORRECT
if (!digestHeaders.TryGetValue("username", out var userName)) return null;

// BUG — throws KeyNotFoundException when header is absent
var userName = digestHeaders["username"];
```

### 6. Username vs. Email Uniqueness Queries
`AssertNoExistingUser` must query the `UserName` field and the `Email` field **independently**. Routing through `GetUserAuthByUserName` is unreliable because it uses heuristics (presence of `@`) to decide which field to search, which can miss conflicts or produce false positives.

---

## Common Modification Scenarios

1. **Adding a new field to `UserAuth` or `UserAuthDetails`** — Because MongoDB stores documents as BSON, new fields are typically backwards-compatible. Ensure that any new query filters or index requirements are reflected in `CollectionsExists`/init logic.

2. **Changing uniqueness rules** (e.g., case-insensitive usernames) — Modify `AssertNoExistingUser` / `AssertNoExistingUserAsync`. Keep sync and async versions in sync. Consider adding a collation to the MongoDB filter.

3. **Adding a new auth repository interface** (e.g., `IQueryUserAuth`) — Implement it on both `MongoDbAuthRepository` and `MongoDbAuthRepositoryAsync`. Add corresponding async MongoDB driver calls; never share a synchronous implementation path from the async class.

4. **Updating MongoDB.Driver dependency** — Check for breaking API changes in `IMongoCollection<T>`, `FilterDefinitionBuilder<T>`, and async cursor handling (`IAsyncCursor<T>`). The driver has historically had breaking changes between major versions.

5. **Adding indexes** — Place index creation in the `CollectionsExists` / initialization path so it runs at app startup. Useful indexes: unique index on `UserAuth.UserName`, unique index on `UserAuth.Email`, index on `UserAuthDetails.UserAuthId`.

6. **Porting a bug fix from sync to async** — Both `MongoDbAuthRepository` and `MongoDbAuthRepositoryAsync` must be updated together. The async class cannot simply call the sync class's methods (sync-over-async); it must replicate the logic using async driver APIs.

7. **Supporting a new ServiceStack auth interface** — Register the implementation against the new interface in the IoC container registration documentation/example and implement both sync and async variants.
