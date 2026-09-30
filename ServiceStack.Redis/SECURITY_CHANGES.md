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

---

## 8. TLS Downgrade & Credential Exposure in `CloneClient()` (`RedisClient`, `RedisPubSubServer`)
- **Severity**: High
- **Description**:
  - `RedisClient.CloneClient()` only copied `Host`, `Port`, `Password`, `Db`, `Username`, and the send/receive timeouts. It dropped `Ssl` and `SslProtocols`, along with `Client`, `NamespacePrefix`, `ConnectTimeout`, `RetryTimeout`, and `IdleTimeOutSecs`.
  - `RedisPubSubServer.HandleFailover` uses `CloneClient()` to publish control messages. Against a TLS-only Redis endpoint, the clone opened a **plain-text** TCP connection and sent `AUTH <username> <password>` unencrypted before the server rejected it (CWE-319).
- **Change**:
  - `CloneClient()` now clones through a full `RedisEndpoint`, preserving `Ssl`, `SslProtocols`, and all other connection settings plus the `ConnectionFilter`.

---

## 9. Credentials Written to Logs (`FailoverTo`, `RedisSentinel`, `RedisSentinelWorker`, verbose logging)
- **Severity**: Medium
- **Description**:
  - `RedisManagerPool`, `PooledRedisClientManager`, and `BasicRedisClientManager` logged the raw host strings passed to `FailoverTo(...)` at `Info` level. These strings can be full connection strings (`password@host` or `?password=`) (CWE-532).
  - `RedisSentinel` and `RedisSentinelWorker` logged sentinel connection strings and `RedisEndpoint.ToString()`, which includes `Password`.
  - With `RedisConfig.EnableVerboseLogging`, `SendUnmanagedExpectSuccess` logged the first 50 bytes of the raw `AUTH` command, which includes the password.
- **Change**:
  - Added the internal `ToSafeHostString()`/`ToSafeHostsString()` helpers, which log only `host:port`, and used them in all failover and sentinel log statements.
  - Verbose logging of connection-time `AUTH` commands now writes `AUTH ***`.

---

## 10. Reply Desynchronization After Cancellation or Unexpected Errors (`RedisNativeClient`)
- **Severity**: Medium
- **Description**:
  - When an async command was cancelled mid-read, or failed with a non-socket exception (e.g. an `IOException` from `SslStream`), the connection stayed open with unread replies. If that client instance was reused, a later command could read the **previous command's reply**, returning another key's data.
  - `Activate()` tried to recover pooled connections by draining `socket.Available` bytes with a raw `socket.Receive`. That can't resync a partially received reply, and on TLS connections it corrupts the `SslStream` by consuming encrypted records.
- **Change**:
  - `CreateConnectionError` now closes the connection, so the next command reconnects instead of reading stale replies.
  - `Activate()` now closes out-of-sync connections, which reconnect lazily, instead of draining raw socket bytes.

---

## 11. Connection Leaks During Failover & Master Discovery (`PooledRedisClientManager`, `RedisResolver`, `RedisSentinelResolver`)
- **Severity**: Medium (resource exhaustion)
- **Description**:
  - When a pool slot changed during failover, `PooledRedisClientManager.GetClient()`/`GetClientAsync()` returned a write client "outside of the pool" but left `ClientManager` set. `Dispose()` therefore handed the client back to a pool that didn't own it, and its socket was never closed. Read clients already handled this correctly.
  - `RedisResolver.GetValidMaster` and `RedisSentinelResolver.CreateRedisClient` opened a test connection to every host during master discovery but never disposed the replica or rejected connections, nor the original non-master client they replaced.
  - Repeated failovers could exhaust client sockets or the server's `maxclients`.
- **Change**:
  - Write clients created outside the pool now clear `ClientManager` (so `Dispose()` closes them) and increment `TotalClientsCreatedOutsidePool`.
  - All discovery connections that aren't returned, and replaced non-master clients, are now disposed.

---

## 12. Pub/Sub Handler Crash Loop & Over-Broad Control Message Filter (`RedisPubSubServer`, `RedisSubscription`)
- **Severity**: Medium (availability)
- **Description**:
  - `RedisPubSubServer` always registered a string `OnMessage` handler on the subscription and called the user's `OnMessage(channel, msg)` without a null check. Consumers that only set `OnMessageBytes` hit a `NullReferenceException` on every non-control message. That tore down the subscription and put it into a restart loop, dropping messages.
  - The byte-level `IsCtrlMessage` check matched any payload **starting with** `CTRL` (e.g. `CTRLALTDEL`), silently dropping application messages. The string path only matches `CTRL` or `CTRL:...`.
- **Change**:
  - `OnMessage` is now optional (`OnMessage?.Invoke`).
  - The byte check now requires `CTRL` to be the whole message or followed by `:`, matching the string check.
  - `RedisSubscription` only decodes message bytes to a string when an `OnMessage` handler is registered.

---

## 13. Reliability & Performance Fixes
- **Retry back-off used XOR instead of a power** (`GetBackOffMultiplier`): `(2 ^ i) * BackOffMultiplier` gave `30ms, 0ms, 10ms, 60ms, 70ms...`, including zero-delay tight retry loops against a failing server. It now uses capped exponential back-off: `BackOffMultiplier * 2^(i-1)`, capped at 5s.
- **Async receive timeout `NullReferenceException`** (`SendReceiveAsync`): the receive-timeout token was taken from the *send* timeout `CancellationTokenSource` (`linkedCts.Token`). With only `ReceiveTimeout` configured, every async command failed with a `NullReferenceException`. When both timeouts were set, the receive timeout was never applied.
- **Static `ServerVersionNumber` reset race**: every `RedisNativeClient` constructor reset the process-wide `ServerVersionNumber` to `0`. Other already-connected clients then read `0` and fell back to legacy commands. For example, `SetValue(key, value, 500ms)` became `SETEX key 0 ...` and failed with `invalid expire time`. The constructor now only overrides the version when `RedisConfig.AssumeServerVersion` is set, and `AssertServerVersionNumber()` detects the version on already-connected clients.
- **Twemproxy fallback leaked a socket**: when `INFO` failed at connect time, `Connect()` recursed without closing the existing connection.
- **`BufferedReader` bypassed its buffer for small reads**: the "large read" check compared `count` against the *caller's* buffer instead of the internal one. Small bulk replies that started at a buffer boundary therefore did a direct socket read and an extra syscall for the trailing CRLF.
- **`RemoveByPattern` loaded every matching key into memory** and issued a single, unbounded `DEL`, which can block redis-server. It now deletes in batches of 1024, matching `RemoveByPatternAsync`.
- **`RemoveByRegex` translated `.+` to `?`**, which matches exactly one character, so it didn't delete keys with longer suffixes. It now translates `.+` to `?*`.
- **`ERR` reply parsing**: a bare `-ERR` reply caused an `ArgumentOutOfRangeException` in `Substring(4)`. This is centralized in `StripErrPrefix`.
- **Pool statistics were inverted**: `GetStats()` counted *empty* slots as `clientsCreated` in `RedisManagerPool` and `PooledRedisClientManager`.
- **Async clients limited to a thread**: with `AssertAccessOnlyOnSameThread`, async clients were limited to the current thread even though async continuations resume on other threads. `RedisManagerPool` did this when reusing a pooled client and `PooledRedisClientManager` when creating one.

Regression tests: `tests/ServiceStack.Redis.Tests/RedisReviewRegressionTests.cs`.
