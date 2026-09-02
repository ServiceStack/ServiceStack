# Security Changes & Remediation Reference (`ServiceStack.Aws`)

This document details security vulnerabilities identified and remediated in `ServiceStack.Aws`.

---

## 1. Silent Error Swallowing in `R2VirtualFiles.WriteFileAsync`
- **Severity**: High (Silent Data Loss)
- **Description**: `R2VirtualFiles.WriteFileAsync` previously caught and logged upload exceptions without rethrowing them. Callers awaiting the write operation received a successful return even when uploads failed (e.g. network drops, authentication failures, quota exceeded, or missing buckets), masking silent data loss.
- **Change**: Added `throw;` in the catch handler so write failures bubble up to calling code.

---

## 2. UserName '@' Prohibition and Email Login Fallback in `DynamoDbAuthRepository`
- **Severity**: High (Authentication Integrity & Multi-Factor Login)
- **Description**: Usernames containing `@` break ServiceStack's convention of distinguishing between usernames and email addresses. Additionally, when a user registered with distinct `UserName` and `Email` fields, lookup by `Email` would return `null` if querying the username global secondary index.
- **Change**:
  - Enforced validation in `ValidateNewUser` that usernames must not contain `@`.
  - Added fallback scan by `Email` in `GetUserAuthByUserName` and `GetUserAuthByUserNameAsync` when `userNameOrEmail` contains `@` and index lookup produces no match.

---

## 3. S3 Connection and Socket Leak in `S3VirtualFile.WritePartialToAsync`
- **Severity**: Medium (Socket & Connection Pool Exhaustion)
- **Description**: `S3VirtualFile.WritePartialToAsync` invoked `AmazonS3.GetObjectAsync` without disposing the `GetObjectResponse` instance, leaving the underlying HTTP response unclosed.
- **Change**: Wrapped `GetObjectResponse` in a `using` block to ensure deterministic disposal.

---

## 4. Default Value Handling & Race Condition in `DynamoDbCacheClient`
- **Severity**: Medium (Cache Integrity)
- **Description**: In `CacheAdd<T>` and `CacheReplace<T>`, key presence was checked using `!Equals(GetValue<T>(key), default(T))`. For value types (`bool`, `int`, numeric types, structs) where the cached value is `false`, `0`, or default, `CacheAdd` mistakenly treated the key as absent and overwrote it, while `CacheReplace` mistakenly treated the key as absent and refused to update it.
- **Change**: Introduced `GetCacheEntry` / `GetCacheEntryAsync` and checked `entry != null` directly to determine key existence regardless of payload value.

---

## 5. Directory Traversal Path Canonicalization in `S3VirtualFiles.SanitizePath`
- **Severity**: Medium (Path Traversal)
- **Description**: `S3VirtualFiles.SanitizePath` did not resolve relative directory segments (such as `..`), allowing potential virtual path traversal.
- **Change**: Updated `SanitizePath` to normalize and resolve relative directory segments with `.ResolvePaths()`.
