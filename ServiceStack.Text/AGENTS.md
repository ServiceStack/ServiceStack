# CONTEXT: ServiceStack.Text

## Purpose

`ServiceStack.Text` is an ultra-fast, zero-dependency .NET text serialization and string manipulation library. It provides high-performance serializers for JSON, JSV (JSON Separated Values), CSV, and XML, along with dynamic JSON inspection, HTTP utilities (`HttpUtils`), and fast reflection/auto-mapping helpers.

Key capabilities:
- **High-Performance JSON Serializer (`JsonSerializer`)**: Allocation-optimized JSON serialization and deserialization outperforming standard serializers.
- **JSV & CSV Serialization (`TypeSerializer`, `CsvSerializer`)**: Human-readable, compact text formats for caching and tabular data export.
- **Dynamic JSON Parsing (`JsonObject`)**: Schema-less, tree-like navigation of JSON documents without ahead-of-time DTO generation.
- **Scoped Configuration (`JsConfig`, `JsConfigScope`)**: Thread-safe, hierarchical serialization configuration overrides per thread or block (`using (JsConfig.With(...))`).
- **Object Auto-Mapping**: High-speed, convention-based object cloning and transformation (`obj.PopulateWith(source)`, `source.ConvertTo<T>()`).

**Target frameworks**: `net472;netstandard2.0;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
                         ServiceStack.Text
                                │
         ┌──────────────────────┼──────────────────────┐
         ▼                      ▼                      ▼
ServiceStack.Interfaces  ServiceStack.Common   ServiceStack.Client
         │                      │                      │
         └──────────────────────┴──────────┬───────────┘
                                           ▼
                                 ServiceStack (Core)
                                           │
                                           ▼
                             All Providers and Subsystems
                       (OrmLite, Redis, Server, AI, Cloud)
```

- **Depends on**: None (pure standalone library; framework assemblies only).
- **Depended on by**: Virtually every project in the ServiceStack suite (`ServiceStack.Common`, `ServiceStack.Client`, `ServiceStack`, `ServiceStack.OrmLite`, `ServiceStack.Redis`, etc.).
- **Role**: Sits at the root of the text processing and serialization stack. Changes here have broad ecosystem impact.

---

## Key Functionality

### 1. Serializers
| Type | Role |
|---|---|
| `JsonSerializer` | Fast JSON serialization to string, stream, or writer (`SerializeToString`, `DeserializeFromString`). |
| `TypeSerializer` | JSV (JSON Separated Values) serializer; ServiceStack's native clean string representation. |
| `CsvSerializer` | Fast tabular CSV serializer with formula injection defenses (`CsvConfig.EscapeFormulas`). |
| `XmlSerializer` | Hardened XML / DataContract serializer with DTD expansion guards. |

### 2. Configuration & Scoping (`JsConfig`)
- Global defaults: `JsConfig.TextCase`, `JsConfig.DateHandler`, `JsConfig.IncludeNullValues`.
- Thread-scoped overrides:
  ```csharp
  using (JsConfig.With(new Config { IncludeNullValues = true, TextCase = TextCase.CamelCase }))
  {
      return JsonSerializer.SerializeToString(dto);
  }
  ```

### 3. Dynamic JSON Inspection
- `JsonObject.Parse(json)`: Inspect un-typed JSON trees.
- `JsonArrayObjects`: Parse and iterate heterogeneous JSON arrays.

### 4. HTTP & Auto-Mapping Utilities
- `HttpUtils`: Async/sync HTTP request helpers, file uploads, and URL combination.
- `AutoMapping`: High-speed object cloning and property population.

---

## Architecture & Design Patterns

### Code-First Serialization
Serializes types without explicit metadata; respects standard attributes (`[DataContract]`, `[DataMember]`, `[IgnoreDataMember]`, `[RuntimeSerializable]`).

### Zero-Allocation Text Parsing
Parses strings directly via `ReadOnlySpan<char>` and memory slices without allocating intermediate strings during token traversal.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Always preserve these security mitigations:

### 1. Deserialization Depth Tracking & Stack Overflow Protection
- **Risk**: Deeply nested JSON/JSV structures cause uncatchable `StackOverflowException` crashing the process.
- **Rule**: All recursive parsers must increment and check depth against `JsConfig.MaxDepth` (default: 50), throwing `SerializationException` when exceeded.

### 2. Untrusted Type Instantiation
- `JsConfig.AllowRuntimeTypeWithAttributesNamed` strictly permits `[RuntimeSerializable]` and `[DataContract]`. `[Serializable]` is excluded by default to avoid arbitrary object instantiation (CWE-502).

### 3. CSV / Spreadsheet Formula Injection (CWE-1236)
- In `CsvWriter`, `CsvConfig.EscapeFormulas` (default: `true`) prefixes cells starting with dangerous characters (`=`, `+`, `-`, `@`, `\t`, `\r`) with a single quote (`'`), preventing code/command execution in Excel.

### 4. XML DTD Expansion & XXE Protection
- `XmlSerializer.DeserializeFromStream` must use `XmlReader` configured with `DtdProcessing.Prohibit` and enforce `MaxCharactersInDocument` limits.

### 5. HTTP Client Windows Identity Leakage
- `HttpUtils.HttpClientHandlerFactory` defaults `UseDefaultCredentials = false` to prevent accidental transmission of ambient Windows NTLM/Kerberos tokens to remote hosts.

### 6. Negative Type Cache Bounding
- In `AssemblyUtils`, cache negative type lookups in `NegativeTypeCache` capped at 1,000 items to prevent memory exhaustion from random type lookups.

---

## Common Modification Scenarios

1. **Configuring Serialization Formatting**
   ```csharp
   JsConfig.Init(new Config {
       TextCase = TextCase.CamelCase,
       DateHandler = DateHandler.ISO8601,
   });
   ```

2. **Custom Type Serializers**
   ```csharp
   JsConfig<CustomType>.SerializeFn = obj => obj.ToCustomFormat();
   JsConfig<CustomType>.DeSerializeFn = str => CustomType.Parse(str);
   ```
