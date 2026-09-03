# Security & Robustness Changes: ServiceStack.Interfaces

This document catalogs type safety, contract correctness, null safety, and robustness enhancements implemented in the `ServiceStack.Interfaces` library.

## 1. Data Model Null Safety & Initialization

- **`AuditBase` Default Initialization**:
  - Initialized required contract properties `CreatedBy` and `ModifiedBy` to `string.Empty` to prevent null dereferences prior to entity persistence / pipeline population.
- **`NavItem` Model**:
  - Marked `NavItem.Label` and `NavItem.Href` as nullable (`string?`) to support flexible navigation items and parameterless instantiation.
- **`RequestLog` Model**:
  - Marked `TraceId` and `OperationName` as nullable (`string?`) to accommodate partial request capture or deferred logging contexts.

## 2. Command Architecture & Dependency Injection Hardening

- **`ICommandAsync` Base Classes**:
  - Initialized injected container dependency `IRequest Request { get; set; } = null!;` across `AsyncCommand`, `SyncCommand`, `AsyncCommandWithResult`, and `SyncCommandWithResult`.
  - Initialized unexecuted command return values `TResult Result { get; protected set; } = default!;`.
  - Prevents spurious `CS8618` warnings while correctly signaling dependency-injected lifecycle semantics.
- **`ScheduledTask` Contracts**:
  - Marked optional task parameters (`Name`, `RequestType`, `Request`, `RequestBody`) as nullable (`string?`), reflecting their configurable nature.

## 3. Attribute & Reflection Hardening

- **`RouteAttribute.ToReflectAttribute`**:
  - Added null-forgiving operators (`!`) on reflection lookups (`GetType().GetProperty(...)!`) when building constructor and property argument dictionaries, guaranteeing non-null keys in `KeyValuePair<PropertyInfo, object>`.
  - Implemented null-safe `Equals(object? obj)` override.
- **`TagAttribute` Parameter Alignment**:
  - Aligned parameter and property to `string? Name`, eliminating `CS8625` (cannot convert null literal to non-nullable reference) when `TagAttribute()` delegates to `this(null)`.
- **`ApiAllowableValuesAttribute` Constructors**:
  - Changed parameter `string name` to `string? name` in overloads delegating with `null` (`this(null, min, max)`, `this(null, values)`, etc.), eliminating recurring compiler warnings.
- **Metadata Attributes (`ApiAttribute`, `ApiResponseAttribute`, `InputAttributeBase`, `FormatAttribute`, `RefAttribute`, `RestrictAttribute`, `IReflectAttributeConverter`)**:
  - Marked optional metadata properties as nullable (`string?`), supporting clean parameterless attribute usage.
  - Initialized `RestrictAttribute.AccessibleToAny` and `VisibleToAny` arrays to empty collections (`[]`), avoiding null dereferences during early attribute inspection.
- **`AttributeExtensions.FirstAttribute`**:
  - Cast to `(TAttr?)` in `FirstAttribute<TAttr>` to eliminate `CS8600`.

## 4. Query & Validation Contract Correctness

- **`IQuery.QueryResponse<T>.Results`**:
  - Initialized default `Results` collection to an empty list (`= []`), preventing null reference exceptions for empty query responses.
- **AutoCrud Query Attributes (`AutoFilterAttribute`, `AutoMapAttribute`, `AutoPopulateAttribute`)**:
  - Marked mapping and filtering fields (`Field`, `To`) as nullable (`string?`), allowing attribute declaration without requiring constructor-only initialization.
- **`ValidationRule` & `IValidateRule`**:
  - Aligned `IValidateRule.Validator` and `ValidateRule.Validator` to `string?`, matching interface contract and implementation.
  - Updated `ValidationRule.Equals(object? obj)` to nullable parameter matching framework signature.

## 5. AI Interfaces

- **`AI/ISpeechToText.cs` & `AI/ITypeChat.cs`**:
  - Marked `TranscriptResult.Transcript`, `ApiResponse`, and `TypeChatResponse.Result` as nullable (`string?`), accurately reflecting unpopulated payloads on failure conditions.
