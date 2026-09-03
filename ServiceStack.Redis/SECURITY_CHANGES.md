# Security Changes & Remediation Reference (`ServiceStack.Redis`)

This document details security vulnerabilities identified and remediated across `ServiceStack.Redis`.

---

## 1. Distributed Lock Mutual Exclusion Violation on Expiration (`RedisLock`, `RedisLock.Async`)
- **Severity**: Critical
- **Description**:
  - `RedisLock.Dispose()` and `RedisLock.DisposeAsync()` previously executed `Remove(key)` unconditionally without verifying whether the disposing instance was still the legitimate owner of the lock.
  - In scenarios where an operation exceeded its timeout (e.g. during GC pauses, heavy I/O, or thread starvation), a second client would acquire the lock. When the initial slow client finally finished its execution and disposed the lock, it deleted the *second client's* active lock from Redis. This allowed a third client to enter the critical section simultaneously, completely breaking distributed mutual exclusion.
- **Change**:
  - Saved the unique timestamp lock value on the `RedisLock` instance upon acquisition.
  - Hardened `Dispose()` and `DisposeAsync()` to perform a check-and-delete (via `WATCH`/`MULTI` transaction) verifying that the key's current value matches the instance's lock token before deleting it.
  - If the lock was lost or expired, the key is left intact, preventing deletion of another process's lock.

---

## 2. Insecure `BinaryFormatter` Deserialization Deprecation (`ObjectSerializer`, `OptimizedObjectSerializer`, `SerializingRedisClient`)
- **Severity**: High
- **Description**:
  - Legacy queue helpers in `Support/Queue` utilized `SerializingRedisClient` which defaulted to `ObjectSerializer` and `OptimizedObjectSerializer`.
  - On .NET Framework (`!NETCORE`), `ObjectSerializer` used `BinaryFormatter.Deserialize` (CWE-502), exposing applications to Remote Code Execution if malicious payloads were inserted into Redis. On modern .NET (`NETCORE`), it returned `null`, silently failing serialization.
- **Change**:
  - Marked `SerializingRedisClient`, `ObjectSerializer`, and `OptimizedObjectSerializer` as `[Obsolete]` with a clear warning explaining that they rely on insecure `BinaryFormatter`. Note that primary Redis clients (`RedisClient`, `RedisNativeClient`) do not use these classes and rely on safe `ServiceStack.Text` serialization.

---

## 3. Internal Control Command Leakage to Subscribers (`RedisPubSubServer`)
- **Severity**: High
- **Description**:
  - In `RedisPubSubServer.cs`, `IsCtrlMessage(byte[] msg)` checked whether an incoming payload was an internal control message (`CTRL:...`).
  - Due to duplicate index references (`msg[0] == 'R' && msg[0] == 'L'`), the condition could never evaluate to `true`.
  - Internal heartbeat pulses and server stop signals were never recognized as control messages and were leaked to user application event handlers via `OnMessageBytes`.
- **Change**:
  - Corrected the byte index verification to `msg[0] == 'C' && msg[1] == 'T' && msg[2] == 'R' && msg[3] == 'L'`.

---

## 4. Redis 6+ ACL Identity Collision in Connection Pools (`RedisEndpoint`)
- **Severity**: Medium
- **Description**:
  - `RedisEndpoint` had a `Username` property for Redis ACL support, but omitted `Username` from `Equals` and `GetHashCode`.
  - Endpoints configured for different users (e.g., an unprivileged reader vs. an administrator) evaluated as identical in dictionaries and hash sets (e.g., in `RedisResolver.allHosts`), risking connection reuse across user permission boundaries.
- **Change**:
  - Added `Username` to both `RedisEndpoint.Equals` and `RedisEndpoint.GetHashCode`.

---

## 5. Loss of ACL Username in Connection String Parsing (`RedisExtensions`, `RedisScripts`)
- **Severity**: Medium
- **Description**:
  - In `RedisExtensions.ToRedisEndpoint`, parsing URI strings in the form `redis://username:password@host:port` assigned `authParts[0]` to `endpoint.Client` instead of `endpoint.Username`.
  - When connecting to Redis 6+ servers with ACLs, `RedisNativeClient` sent `AUTH <password>` rather than `AUTH <username> <password>`, failing authentication or authenticating as the `default` user.
  - `RedisScripts.redisToConnectionString` dropped `username` when converting dictionary configurations back to connection strings.
- **Change**:
  - Populated `endpoint.Username` (and URL-decoded credentials) in `ToRedisEndpoint`.
  - Added `username` support to `RedisScripts.redisToConnectionString`.

---

## 6. Master-Replica Routing Inversion in Cache Client (`BasicRedisClientManager`)
- **Severity**: Medium
- **Description**:
  - In `BasicRedisClientManager.ICacheClient.cs`, `Remove(string key)` called `GetReadOnlyCacheClient()`.
  - Because `Remove` is a mutating write operation (DEL), calling `Remove(key)` against a read-replica cluster resulted in `RedisResponseException: READONLY You can't write against a read only replica`.
- **Change**:
  - Updated `BasicRedisClientManager.ICacheClient.Remove` to route to `GetCacheClient()`, matching `ICacheClientAsync.RemoveAsync`.

---

## 7. Bounds Checking & Safe Token Parsing (`RedisSubscription`, `RedisClient_Admin`, `RedisDataInfoExtensions`)
- **Severity**: Low / Robustness
- **Description**:
  - `RedisSubscription` and `RedisSubscription.Async` stepped through incoming packet chunks without verifying that `i + componentsPerMsg <= multiBytes.Length`, and used `int.Parse` without error protection.
  - `RedisClient.GetClientsInfoParse` assumed every space-separated token in `CLIENT LIST` had an `=` sign, risking `IndexOutOfRangeException`.
  - `RedisDataInfoExtensions.Parse` used `.Add()` on dictionaries when parsing Redis `INFO` results, throwing `ArgumentException` on duplicate keys.
- **Change**:
  - Added array bounds checking and `int.TryParse` in `RedisSubscription`.
  - Handled key-value tokens without `=` safely in `GetClientsInfoParse`.
  - Used dictionary indexer assignments in `RedisDataInfoExtensions.Parse` to safely absorb duplicate sections or keys.
