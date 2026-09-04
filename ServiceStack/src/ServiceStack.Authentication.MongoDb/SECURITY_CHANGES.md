# Security & Hardening Changes: ServiceStack.Authentication.MongoDb

## Overview
This document summarizes the security vulnerabilities, thread-safety, reliability, and correctness fixes implemented in `ServiceStack.Authentication.MongoDb`.

---

### 1. Fix User Profile Update Logic Error (`AssertNoExistingUser`)
- **Severity**: High
- **Issue**: `UpdateUserAuth(existingUser, newUser)` and `UpdateUserAuthAsync(existingUser, newUser)` invoked `AssertNoExistingUser(mongoDatabase, newUser)` without passing `existingUser`. Because the user being updated already existed in the collection with that username/email, any update attempt that retained the same username or email failed with `ArgumentException("User already exists")`.
- **Remediation**: Corrected calls to `AssertNoExistingUser(mongoDatabase, newUser, existingUser)` and `AssertNoExistingUserAsync(mongoDatabase, newUser, existingUser, token)`, allowing valid profile and password updates.

---

### 2. Elimination of Sync-Over-Async Thread Pool Starvation
- **Severity**: Medium
- **Issue**: In `MongoDbAuthRepositoryAsync.cs`, `CreateOrMergeAuthSessionAsync` executed `providerCollection.Find(u => u.UserAuthId == userAuthId).FirstOrDefault()`, which is a synchronous MongoDB driver call inside an asynchronous method. Under heavy load, this could exhaust .NET thread pool worker threads.
- **Remediation**: Replaced with `(await providerCollection.FindAsync(u => u.UserAuthId == userAuthId, cancellationToken: token).ConfigAwait()).FirstOrDefault()`.

---

### 3. Orphaned OAuth Provider Record Cleanup (`DeleteMany`)
- **Severity**: Medium
- **Issue**: `DeleteUserAuth` and `DeleteUserAuthAsync` used `DeleteOne` and `DeleteOneAsync` on `UserAuthDetails`. Users with multiple linked OAuth providers (e.g. Google, GitHub, Twitter) had orphaned OAuth records left behind upon account deletion, creating data leaks or potential account collisions if IDs were reused.
- **Remediation**: Replaced with `providerCollection.DeleteMany(u => u.UserAuthId == intUserId)` and `await providerCollection.DeleteManyAsync(u => u.UserAuthId == intUserId, token).ConfigAwait()`.

---

### 4. FormatException DOS / Unhandled Exception Protection on User ID Parsing
- **Severity**: Medium
- **Issue**: User IDs passed to `GetUserAuth`, `GetUserAuthDetails`, `DeleteUserAuth`, and `SaveUserAuth` (and their async counterparts) were parsed using raw `int.Parse(userAuthId)`. Null, empty, non-numeric, or malformed ID strings crashed request processing with unhandled `FormatException`.
- **Remediation**: Replaced all instances with `int.TryParse(userAuthId, out var intUserId)`, returning `null` or gracefully ignoring invalid IDs.

---

### 5. HTTP Digest Authentication Null Pointer Safety
- **Severity**: Low / Reliability
- **Issue**: `TryAuthenticate(Dictionary<string, string> digestHeaders, ...)` and its async counterpart accessed `digestHeaders["username"]` directly without verifying whether `digestHeaders` was null or whether the `"username"` key existed, resulting in `NullReferenceException` or `KeyNotFoundException`.
- **Remediation**: Added null checks: `if (digestHeaders == null || !digestHeaders.TryGetValue("username", out var userName) || string.IsNullOrEmpty(userName)) return false;`.

---

### 6. Accurate Uniqueness Validation for Usernames & Emails
- **Severity**: Low / Correctness
- **Issue**: `AssertNoExistingUser` routed username checks through `GetUserAuthByUserName`, which treated any string containing `@` as an email lookup. If a user had an `@` symbol in their username (or vice versa), collision checking evaluated against the wrong field.
- **Remediation**: Updated `AssertNoExistingUser` and `AssertNoExistingUserAsync` to query MongoDB directly for matching `UserName` and matching `Email` separately, while correctly excluding `existingUser.Id`.

---

### 7. Modernized Collection Verification and Initialization
- **Severity**: Quality / Reliability
- **Issue**:
  - `CollectionsExists()` checked `collections.Count > 0` instead of verifying all required collections (`UserAuth`, `UserAuthDetails`, `ApiKey`).
  - Optional `createMissingCollections = false` was missing from the `MongoDbAuthRepository` constructor.
  - Async collection check `CollectionsExistsAsync` was not implemented.
- **Remediation**:
  - Made `createMissingCollections = false` optional in constructor.
  - Updated `CollectionsExists()` to check `RequiredCollections.TrueForAll(name => collections.Contains(name))`.
  - Implemented `CollectionsExistsAsync(CancellationToken token = default)`.
  - Conditioned legacy .NET Framework assembly references in `.csproj` to `net472`, eliminating 522 build warnings on modern .NET targets.
