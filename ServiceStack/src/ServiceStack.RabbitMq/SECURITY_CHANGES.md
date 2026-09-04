# Security and Reliability Remediation: `ServiceStack.RabbitMq`

## Summary
Audit and hardening of `ServiceStack.RabbitMq` across all targets (`net472`, `net6.0`, `net8.0`, `net10.0`), addressing clock drift and timeout arithmetic in `SharedQueue`, `IEnumerator<T>` state conformance, MemoryStream leak prevention during custom content-type deserialization, non-Guid AMQP header resilience, 404 exchange recovery logic in message publishing, thread safety for connection initialization and declared queue tracking, and cross-platform thread shutdown.

---

## Remediations

### 1. Monotonic Timeout and Clock Drift Defense (`SharedQueue.cs`)
- **Issue**: `SharedQueue<T>.Dequeue(TimeSpan timeout, out T result)` used `DateTime.Now` arithmetic to compute remaining wait time. System clock adjustments (such as NTP time syncs or daylight saving time transitions) could cause premature timeouts or indefinite hangs.
- **Fix**: Replaced `DateTime.Now` elapsed time calculation with `Stopwatch.StartNew()` (`sw.Elapsed`), ensuring high-resolution monotonic time measurement immune to wall-clock shifts across all supported frameworks.

### 2. Standard Enumerator State Conformance (`SharedQueue.cs`)
- **Issue**: `SharedQueueEnumerator<T>.Current` checked `if (_current == null) throw new InvalidOperationException();`. For reference types, enqueuing a legitimate `null` value caused `Current` to throw despite `MoveNext()` succeeding. For value types (e.g. `int`), reading `Current` before `MoveNext()` yielded `default(T)` instead of throwing `InvalidOperationException`.
- **Fix**: Added explicit `_hasCurrent` state tracking to `SharedQueueEnumerator<T>`, guaranteeing standard `IEnumerator<T>` semantics across all generic type arguments.

### 3. Stream Disposal & Allocation Optimization in Message Parsing (`RabbitMqExtensions.cs`)
- **Issue**: In `ToMessage<T>`, when deserializing payloads with non-JSON content types, `MemoryStreamFactory.GetStream(...)` was disposed manually via `ms.Dispose()`. If the content-type deserializer threw an exception, the rented stream was never disposed or returned to the pool, leaking memory.
- **Fix**: Wrapped stream acquisition in `using var ms = MemoryStreamFactory.GetStream(...)`. In addition, enabled zero-allocation UTF-8 string decoding directly from `msgResult.Body.Span` on .NET 6+ (`#if NET6_0_OR_GREATER`).

### 4. Resilient Guid Parsing on AMQP Identifiers (`RabbitMqExtensions.cs`)
- **Issue**: `props.MessageId` and `props.CorrelationId` were parsed using `Guid.Parse(...)`. When interacting with external AMQP producers or message brokers that supply non-Guid string identifiers (e.g. sequential integers, alphanumeric tags, or custom prefixes), `Guid.Parse` threw unhandled `FormatException` errors.
- **Fix**: Used `Guid.TryParse` for both `MessageId` and `CorrelationId`. If parsing fails, `Message.Id` safely defaults to `Guid.Empty` and `Message.ReplyId` remains `null`, preventing message ingestion failures while preserving raw headers in `Message.Meta`.

### 5. 404 Exchange Recovery Return & Thread-Safe Queue Tracking (`RabbitMqProducer.cs`)
- **Issue**:
  - In `RabbitMqProducer.PublishMessage`, catching `OperationInterruptedException` with HTTP 404 exchange-not-found recovered by declaring the exchange and re-publishing, but lacked a `return;` statement, causing it to fall through and re-throw the exception to the caller despite successful re-publish.
  - Declared queues were cached in a `static HashSet<string> Queues` that was mutated via reassignment without thread synchronization, leading to race conditions and lost registrations under concurrent publishers.
- **Fix**:
  - Added `return;` after successfully re-publishing to the declared exchange upon 404 recovery.
  - Converted `Queues` to a `ConcurrentDictionary<string, bool>` with lock-free lookups and atomic additions.
  - Added safe disposal of closed channels in the `Channel` property getter before opening replacement channels.
  - Added double-checked locking around `RabbitMqProducer.Connection` and `RabbitMqServer.Connection`.

### 6. Safe Delivery Tag Handling & Monotonic Polling (`RabbitMqQueueClient.cs`)
- **Issue**: `Ack` and `Nak` used `ulong.Parse(message.Tag)` without null or format guards, risking `ArgumentNullException` and `FormatException`. `Get<T>` used `DateTime.UtcNow` arithmetic for timeout polling.
- **Fix**: Validated and parsed delivery tags using `ulong.TryParse` in `Ack` and `Nak`. Migrated `Get<T>` timeout checking to `Stopwatch.StartNew()`.

### 7. Cross-Platform Thread Shutdown (`RabbitMqServer.cs`, `RabbitMqWorker.cs`)
- **Issue**: `KillBgThreadIfExists` invoked `bgThread.Abort()`, which throws `PlatformNotSupportedException` at runtime on .NET Core / .NET 6+.
- **Fix**: Guarded `bgThread.Abort()` with `#if NETFRAMEWORK` and caught `PlatformNotSupportedException` on modern .NET runtimes.
