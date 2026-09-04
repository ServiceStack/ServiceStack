# Security & Hardening Changes: ServiceStack.Caching.Memcached

## Overview
This document summarizes the security vulnerabilities, bug fixes, reliability improvements, and input validation enhancements implemented in `ServiceStack.Caching.Memcached`.

---

### 1. Fix Leaked Wrapper in CAS Operation `Get(string key, out ulong ucas)`
- **Severity**: High / Correctness & Data Leakage
- **Issue**: `Get(string key, out ulong ucas)` returned `result.Result` directly, which was an instance of `MemcachedValueWrapper` instead of the actual cached payload (`result.Result.Value`). This leaked internal wrapper types to consumers and broke callers expecting the cached object. Additionally, unlike all other methods in `MemcachedClientCache`, this method was not wrapped in `Execute(...)` for execution timing and error logging.
- **Remediation**:
  - Wrapped `_client.GetWithCas<MemcachedValueWrapper>(key)` in `Execute(...)`.
  - Returned `result.Result.Value` if the CAS result was found.
  - Added null guard for `key == null`.

---

### 2. Constructor Input Hardening & Host / IPv6 Parsing
- **Severity**: Medium / Denial of Service & Crash Resilience
- **Issue**:
  - `MemcachedClientCache(IEnumerable<string> hosts)` threw `NullReferenceException` if `hosts` was null.
  - Splitting host addresses solely on `:` failed on IPv6 addresses (e.g. `[::1]:11211` or `[::1]`).
  - Using raw `int.Parse` on the port threw `FormatException` or `OverflowException` on malformed port strings.
  - Line 49 threw an unformatted exception string: `throw new ArgumentException("'{0}' is not a valid host IP Address: e.g. '127.0.0.0[:11211]'");` without formatting the host parameter.
  - Constructors taking `IEnumerable<IPEndPoint>` and `IMemcachedClientConfiguration` lacked null parameter checks.
- **Remediation**:
  - Added `ArgumentNullException` validation for null collections and configurations.
  - Implemented bracket-aware IPv6 and IPv4 host:port parsing.
  - Validated port numbers with `int.TryParse` ensuring ports fall within range `1..65535`.
  - Fixed exception message string interpolation.

---

### 3. Collection & Operation Null Safety
- **Severity**: Low / Robustness
- **Issue**:
  - Calling `GetAll<T>(keys)`, `GetAll(keys)`, `SetAll<T>(values)`, or `RemoveAll(keys)` with null inputs caused `NullReferenceException`.
  - In `GetAll(keys, out casValues)`, direct casting `(MemcachedValueWrapper)casResult.Value.Result` risked `InvalidCastException` if non-wrapper objects were returned.
- **Remediation**:
  - Added null collection checks returning empty dictionaries or early returns.
  - Replaced unsafe cast with pattern matching: `casResult.Value.Result is MemcachedValueWrapper wrapper ? wrapper.Value : casResult.Value.Result`.
  - Added null key checks across `Remove`, `Get<T>`, `Increment`, `Decrement`, `Add`, `Set`, `Replace`, and `CheckAndSet`.

---

### 4. `MemcachedValueWrapper` Resilience & Deserialization Safety
- **Severity**: Medium / Exception Safety
- **Issue**:
  - Calling the `Value` getter when `ValueType` was null threw `ArgumentNullException` from `JsonSerializer.DeserializeFromString(JsonString, ValueType)`.
  - Malformed or corrupt JSON payloads in the cache caused unhandled deserialization exceptions.
  - Wrapping an already wrapped object (`new MemcachedValueWrapper(wrapper)`) created nested wrappers.
- **Remediation**:
  - Unwrapped nested `MemcachedValueWrapper` instances in the constructor.
  - Checked `ValueType != null` before typed deserialization.
  - Added graceful fallback to `JsonSerializer.DeserializeFromString<object>(JsonString)` and raw string return on deserialization failure.

---

### 5. `EnyimLoggerWarpper` Null Safety & Typo Alias
- **Severity**: Low / Robustness & API Ergonomics
- **Issue**:
  - If `serviceStackLogger` passed to `EnyimLoggerWarpper` was null, every subsequent logging invocation threw `NullReferenceException`.
  - The class name contained a typo (`Warpper` instead of `Wrapper`).
- **Remediation**:
  - Defaulted null logger to `new NullDebugLogger(typeof(EnyimLoggerWarpper))`.
  - Introduced `public class EnyimLoggerWrapper : EnyimLoggerWarpper` alias while maintaining `EnyimLoggerWarpper` for full backwards binary and source compatibility.
