# CONTEXT: ServiceStack.Interfaces

## Purpose

`ServiceStack.Interfaces` is the foundational, **implementation-free contract library** for the entire ServiceStack ecosystem. It contains all core interfaces, marker contracts, metadata attributes, and primitive DTO models needed to define ServiceStack services, message contracts (DTOs), service clients, and provider abstractions without depending on any concrete framework implementation or HTTP pipeline logic.

Because it has virtually zero third-party dependencies, DTO assemblies can reference `ServiceStack.Interfaces` alone to share service contracts cleanly across client, server, and external consumer boundaries.

**Target frameworks**: `net472;netstandard2.0;net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
             ┌────────────────────────┐
             │ ServiceStack.Interfaces│
             └───────────┬────────────┘
                         │
         ┌───────────────┼───────────────┐
         ▼               ▼               ▼
ServiceStack.Text  ServiceStack.Common  ServiceStack.Client
         │               │               │
         └───────────────┼───────────────┘
                         ▼
                    ServiceStack (Core Framework)
                         │
         ┌───────────────┼───────────────┐
         ▼               ▼               ▼
ServiceStack.Server  ServiceStack.AI  All Other Providers
```

- **Depends on**: None (framework standard libraries; `Microsoft.Bcl.AsyncInterfaces` and `System.Threading.Tasks.Extensions` on older TFMs).
- **Depended on by**: Practically every package in the ServiceStack suite (`ServiceStack`, `ServiceStack.Client`, `ServiceStack.Common`, `ServiceStack.Server`, `ServiceStack.AI`, `ServiceStack.Jobs`, etc.), as well as user client DTO assemblies.
- **Role**: Defines the wire contracts and abstractions; must remain strictly implementation-free, lightweight, and backwards-compatible.

---

## Key Functionality

### Core Service & Message Contracts
| Type | Description |
|---|---|
| `IReturn<T>` / `IReturnVoid` | Marker interfaces on Request DTOs specifying their expected response type. Fundamental to end-to-end typed clients. |
| `IService` | Base marker interface that service implementations implement. |
| `IService<T>` / `IServiceAsync<T>` | Generic service contract interfaces for sync and async request handling. |
| `IServiceGateway` / `IServiceGatewayAsync` | Gateway abstraction allowing services to dispatch internal requests to other services. |
| `ICommandAsync<T>` / `AsyncCommand` | Command pattern abstractions for executing self-contained transactional operations. |

### Routing & Metadata Attributes
| Attribute | Purpose |
|---|---|
| `[Route(path, verbs)]` | Defines custom REST routes and HTTP verbs on Request DTOs. |
| `[Api(description)]` | Documents service endpoints for metadata pages, OpenAPI, and Postman. |
| `[ApiMember]` / `[ApiAllowableValues]` | Decorates DTO properties with documentation, constraints, and allowable values. |
| `[Validate]` / `[ValidateRequest]` | Declarative validation attributes evaluated before request execution. |
| `[Restrict]` | Controls service visibility and accessibility based on internal/external networks, localhost, or scenario. |
| `[Tag]`, `[Notes]`, `[Format]` | Visual organization and presentation attributes for metadata and UI generation. |
| `[Tool]`, `[Mcp]` | AI agent and Model Context Protocol (MCP) tool export annotations on DTOs. |

### Domain & Provider Abstractions
- **Auth (`ServiceStack.Auth`)**: `IUserAuth`, `IUserAuthDetails`, `IAuthRepository`, `IUserAuthRepositoryAsync`, `IAuthSession`, `IApiKey`, `IManageApiKeys`.
- **Caching (`ServiceStack.Caching`)**: `ICacheClient`, `ICacheClientAsync`, `IRemoveByPattern`.
- **Messaging (`ServiceStack.Messaging`)**: `IMessage<T>`, `IMessageFactory`, `IMessageProducer`, `IMessageQueueClient`.
- **AutoQuery (`ServiceStack.IQuery`)**: `IQueryDb<From, Into>`, `IQuery<From>`, `QueryResponse<T>`, `QueryBase`.
- **Virtual File System (`ServiceStack.IO`)**: `IVirtualFile`, `IVirtualDirectory`, `IVirtualPathProvider`, `IVirtualFiles`.
- **Logging (`ServiceStack.Logging`)**: `ILog`, `ILogFactory`.
- **Configuration (`ServiceStack.Configuration`)**: `IAppSettings`, `IContainer`.

### Standard Response & Error DTOs
- `ResponseStatus`: Structured status and error container containing `ErrorCode`, `Message`, `StackTrace`, and child `ResponseError` field violations.
- `IHasResponseStatus`: Interface implemented by response DTOs providing standard `ResponseStatus` properties.
- `AuditBase`: Base class for entities carrying audit trails (`CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy`).
- `EmptyResponse`: Canonical response DTO for operations with no payload.

---

## Architecture & Design Patterns

### Pure Contract Pattern
`ServiceStack.Interfaces` contains no service locator calls, no HTTP listeners, no I/O drivers, and no serialization engines. All types are interfaces, abstract base classes, lightweight DTOs, or attributes.

### Declarative Metadata & Reflection Conversion
Attributes like `[Route]` and `[ApiMember]` expose converter hooks (such as `IReflectAttributeConverter` and `ToReflectAttribute()`) enabling dynamic inspection, runtime route generation, and OpenAPI schema generation without coupling to concrete compilers.

### Sync / Async Symmetry
Where provider interfaces support asynchronous operations, pair interfaces are defined (e.g., `IRestClient` / `IRestClientAsync`, `IUserAuthRepository` / `IUserAuthRepositoryAsync`, `ICacheClient` / `ICacheClientAsync`). Interfaces prefer `ValueTask` or `Task` returns and accept `CancellationToken`.

---

## Security & Reliability Considerations

> Sourced from past stability and null-safety fixes documented in `SECURITY_CHANGES.md`. Preserve these rules in all contract updates.

### 1. Default Initialization of DTO Collections and Audit Strings
- **Risk**: Deserializers or manual instantiation leaving collection or string properties `null` causes unexpected `NullReferenceException` downstream.
- **Rule**:
  - `AuditBase.CreatedBy` and `AuditBase.ModifiedBy` must initialize to `string.Empty`.
  - `IQuery.QueryResponse<T>.Results` must default to an empty list (`= []`).
  - `RestrictAttribute.AccessibleToAny` and `VisibleToAny` arrays must default to empty arrays (`[]`).

### 2. Dependency Injection Lifecycle Properties on Command Classes
- **Risk**: Marking injected properties non-nullable without initialization triggers compiler `CS8618` warnings or runtime failures during dependency resolution.
- **Rule**: Base classes `AsyncCommand`, `SyncCommand`, `AsyncCommandWithResult`, and `SyncCommandWithResult` must use the null-forgiving default:
  ```csharp
  public IRequest Request { get; set; } = null!;
  public TResult Result { get; protected set; } = default!;
  ```

### 3. Attribute Null Safety and Nullable Parameter Alignment
- **Risk**: Attribute constructors delegating with `null` (e.g., `TagAttribute() : this(null)`) generate `CS8625` unless the parameter is explicitly declared nullable (`string?`).
- **Rule**: Metadata attributes (`TagAttribute`, `ApiAllowableValuesAttribute`, `ApiAttribute`, `ValidationRule`) must type optional name/value parameters as nullable (`string?`).

### 4. Route Attribute Reflection Argument Safety
- **Risk**: `RouteAttribute.ToReflectAttribute` populates argument dictionaries via reflection. Using unsafe reflection lookups can yield null keys or null dereferences.
- **Rule**: Ensure reflection property lookups use null-forgiving or null-checked patterns, and ensure `Equals(object? obj)` handles null and mismatched types gracefully.

---

## Common Modification Scenarios

1. **Adding a New Service Marker or Contract Interface**
   - Place in the appropriate namespace folder (e.g., `Commands/`, `Auth/`, `IO/`).
   - Keep interfaces minimal and focused (Interface Segregation Principle).
   - Provide async variants accepting `CancellationToken` alongside sync variants when I/O is involved.

2. **Adding or Extending a Metadata Attribute**
   - Inherit from `AttributeBase` or standard `Attribute`.
   - Ensure optional string parameters and properties are typed `string?`.
   - If providing default parameterless constructors, ensure constructor chaining does not pass `null` to non-nullable parameters.
   - If the attribute needs runtime introspection in metadata services, implement `IReflectAttributeConverter`.

3. **Updating Core DTO Classes (e.g., `ResponseStatus`, `AuditBase`, `NavItem`)**
   - Ensure all collections are initialized to non-null empty defaults.
   - Do not remove or rename existing public properties — this is the public API contract across client and server boundaries.
   - Retain data contract attributes (`[DataContract]`, `[DataMember]`) where required for XML/Protobuf compatibility.

4. **Adding Target Frameworks or Conditional Compilation**
   - `ServiceStack.Interfaces` multi-targets from `net472` to modern .NET (`net6.0`, `net8.0`, `net10.0`).
   - Keep framework-specific `#if` defines minimal. Favor standard .NET standard abstractions.
