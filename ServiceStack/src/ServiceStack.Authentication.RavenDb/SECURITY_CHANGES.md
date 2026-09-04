# Security & Hardening Changes: ServiceStack.Authentication.RavenDb

## Overview
This document summarizes the security vulnerabilities, thread-safety, reliability, and correctness fixes implemented in `ServiceStack.Authentication.RavenDb`.

---

### 1. `RavenIdConverter` Robustness & Exception Protection
- **Severity**: High / Denial of Service
- **Issue**: `RavenIdConverter.ToInt` assumed composite IDs formatted strictly with a slash (`/`) and hyphen (`-`). Supplying numeric IDs (`"100"`), single-segment keys (`"users/1"`), deep path keys (`"databases/mydb/docs/users/1-A"`), or malformed/empty strings threw unhandled `IndexOutOfRangeException` or `NullReferenceException`. In `ToString`, negative values and out-of-range ASCII offsets caused invalid characters or exceptions.
- **Remediation**:
  - Implemented `TryToInt(string ravenId, out int id)` to safely parse composite Raven keys, single-segment keys, direct integer strings, and deep paths.
  - Made `ToInt` throw descriptive `FormatException` instead of runtime array indexing exceptions.
  - Handled case-insensitive cluster tags (`'a'` -> `'A'`).
  - Added bounds checking in `ToString` (`id < 0` fallback to 0, ASCII offset clamping between 0 and 25, trailing slash trimming).

---

### 2. Elimination of `FormatException` in Session UserAuthId Deserialization
- **Severity**: High / Crash Resilience
- **Issue**: In `LoadOrCreateFromSession` and `LoadOrCreateFromSessionAsync`, `RavenIdConverter.ToString(UserAuthCollectionName, int.Parse(authSession.UserAuthId))` called raw `int.Parse` on `authSession.UserAuthId`. However, `RavenDbUserAuthRepository` sets `authSession.UserAuthId` to the Raven document key string (e.g. `"RavenUserAuths/1-A"`). Calling `int.Parse` on a Raven key crashed authentication session saves with an unhandled `FormatException`. Furthermore, if the user document was not found, a subsequent `NullReferenceException` occurred.
- **Remediation**:
  - Checked whether `authSession.UserAuthId` is already a collection document key (contains `/`).
  - Applied `int.TryParse` when converting numeric IDs to Raven keys.
  - Added fallback `userAuth = authSession.ConvertTo<TUserAuth>()` when user load returns null.

---

### 3. Null Entity and Cascade Deletion Safety in `DeleteUserAuth`
- **Severity**: Medium / Reliability
- **Issue**:
  - Calling `session.Delete(userAuth)` threw `ArgumentNullException` if the user did not exist or had already been deleted.
  - `session.Query<UserAuth_By_UserAuthDetails.Result>()` queried index projection results instead of tracked entity documents (`TUserAuthDetails`), leading to failures during cascade deletion.
- **Remediation**:
  - Added `if (userAuth != null) session.Delete(userAuth);` across sync and async implementations.
  - Added `.OfType<TUserAuthDetails>()` to query tracked entity documents before invoking `session.Delete`.
  - Converted numeric IDs to Raven keys if a caller provided an integer ID.

---

### 4. Prevention of Static Populator Delegate Leaks
- **Severity**: Medium / Memory Leak & Thread Safety
- **Issue**: On every instantiation of `RavenDbUserAuthRepository`, `RegisterPopulator()` wrapped `AutoMappingUtils.GetPopulator()` in a new lambda. In DI environments or repeated instantiations, this formed an unbounded nested delegate chain, causing memory leaks and risk of stack overflow during auto-mapping.
- **Remediation**: Guarded `RegisterPopulator()` with `isPopulatorRegistered` static flag and double-check locking (`populatorLock`), ensuring registration occurs exactly once.

---

### 5. Thread-Safe Index Creation & Optional Initialization
- **Severity**: Medium / Concurrency
- **Issue**: `EnsureThatUniqueIndexesAreCreated` inspected static `IsInitialized` without synchronization, allowing race conditions during startup. Also, repositories could not be instantiated in environments where automatic index creation was undesirable or pre-configured.
- **Remediation**:
  - Added `lock (initLock)` around `EnsureThatUniqueIndexesAreCreated`.
  - Added optional `bool createIndexes = true` parameter to `RavenDbUserAuthRepository` constructors.

---

### 6. HTTP Digest Authentication Null Safety
- **Severity**: Low / Reliability
- **Issue**: Direct dictionary lookup `digestHeaders["username"]` threw `NullReferenceException` or `KeyNotFoundException` on missing or null headers.
- **Remediation**: Added null guards and `digestHeaders.TryGetValue("username", out var userName)` validation in both sync and async `TryAuthenticate`.

---

### 7. Parameter Validation and Cancellation Token Propagation
- **Severity**: Quality / Reliability
- **Issue**:
  - Missing argument validation on `CreateUserAuth`, `UpdateUserAuth`, and `SaveUserAuth` caused late `NullReferenceException`.
  - Async methods in `IManageApiKeysAsync` and query helpers ignored `CancellationToken` and missed `.ConfigAwait()`.
- **Remediation**:
  - Added early `ArgumentNullException` checks on required user parameters.
  - Propagated `token` and `.ConfigAwait()` across `StoreAsync`, `SaveChangesAsync`, `ToListAsync`, and `LoadAsync`.
