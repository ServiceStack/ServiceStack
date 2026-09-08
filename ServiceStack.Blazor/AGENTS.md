# CONTEXT: ServiceStack.Blazor

## Purpose

`ServiceStack.Blazor` is a comprehensive Blazor component library providing rich, metadata-driven UI components for Blazor Server, Blazor WebAssembly (WASM), and Blazor Hybrid (MAUI).

Key capabilities include:
- **AutoQuery & AutoCrud Grids (`AutoQueryGrid`)**: Instant, feature-complete CRUD data grids generated directly from ServiceStack AutoQuery DTOs with search, filtering, sorting, pagination, and export.
- **Dynamic Forms (`AutoForm`, `AutoCreateForm`, `AutoEditForm`)**: Declarative forms generated automatically from C# Request DTOs and data annotations (`[Input]`, `[Validate]`, `[Field]`).
- **Tailwind & Bootstrap Component System**: Ready-to-use modern UI primitives (Buttons, Navbars, Modals, Breadcrumbs, Alerts, Dropdowns, SlideOver, File Uploaders).
- **Integrated Blazor Authentication (`AuthBlazorComponentBase`)**: Built-in authorization, role and permission enforcement, and session synchronization with ServiceStack auth providers.

**Target frameworks**: `net6.0;net8.0;net10.0`

---

## Role in ServiceStack Ecosystem

```
ServiceStack.Interfaces     ServiceStack.Client     Microsoft.AspNetCore.Components
         │                          │                               │
         └──────────────────────────┼───────────────────────────────┘
                                    ▼
                          ServiceStack.Blazor
                                    │
           ┌────────────────────────┴────────────────────────┐
           ▼                                                 ▼
Blazor WebAssembly & Static SSR                   Blazor Server & MAUI Hybrid
(Instant AutoQuery Grids & Dynamic Forms)         (Shared Components & Full-Stack UI)
```

- **Depends on**: `ServiceStack.Client`, `ServiceStack.Interfaces`, `Microsoft.AspNetCore.Components.WebAssembly`, `Microsoft.AspNetCore.Components.Authorization`.
- **Depended on by**: Modern full-stack C# web applications, admin portals, and Blazor frontends communicating with ServiceStack APIs.
- **Role**: The frontend component suite for ServiceStack in the .NET / Blazor ecosystem.

---

## Key Functionality

### 1. Data-Driven Components
| Component | Role |
|---|---|
| `AutoQueryGrid<TFrom, TInto>` | Full-featured data grid bound to AutoQuery services with CRUD modals and column customizations. |
| `AutoForm<T>` | Dynamic forms generating input controls matching DTO properties and validation rules. |
| `AutoCreateForm<T>` / `AutoEditForm<T>` | Specialized forms for entity creation and modification with optimistic updates. |
| `DynamicInputBase` | Base class for input controls with automatic attribute sanitization. |

### 2. Authorization & Navigation
- **`AuthBlazorComponentBase`**: Component base verifying current user roles (`RequiredRoles`) and permissions (`RequiredPermissions`).
- **`NavigationUtils`**: Open-redirect safe return URL navigation and query parameter binding.

### 3. UI Infrastructure
- Tailwind CSS defaults and themes (`CssDefaults`).
- JavaScript interop helpers and circuit-isolated render actions.

---

## Architecture & Design Patterns

### Metadata-First UI Generation
Components inspect runtime service metadata (AutoQuery DTOs, route definitions, validation attributes) to configure form inputs, table columns, and permissions automatically without repetitive markup.

### Circuit Isolation in Blazor Server
Avoids static component state. All queues and render actions are scoped to component instances to prevent cross-circuit data contamination across concurrent Blazor Server sessions.

---

## Security & Reliability Considerations

> Sourced from `SECURITY_CHANGES.md`. Always observe these constraints:

### 1. Negation in Authorization Checks
- In `AuthBlazorComponentBase.CanAccess`, ensure required role and permission checks use the negation operator (`!op.RequiredRoles.All(...)`) so access is denied only when roles/permissions are missing.

### 2. Open Redirect Prevention (CWE-601)
- `NavigationUtils.GetReturnUrl` must validate that URLs represent local application paths using `IsLocalUrl()` (must begin with `/` and not `//` or `/\`), defaulting to `"/"` on external URLs.

### 3. Cross-Circuit State Isolation in Blazor Server
- Never declare UI render actions or callback queues as `static`. Use instance-level `ConcurrentQueue<Func<IJSRuntime, Task>>` to prevent User A's render actions from executing in User B's circuit.

### 4. DOM Event Handler Attribute Sanitization
- In `DynamicInputBase.AllAttributes`, sanitize dangerous DOM event handlers (`onclick`, `onload`) by inspecting the attribute *name* (`key.StartsWith("on")`), not the attribute value.

### 5. XSS Prevention in HTML Dump Helpers
- In `BlazorUtils.FormatValueAsHtml` and `HtmlUtils`, always HTML-encode dictionary keys, scalar values, element IDs, and class names before interpolating into markup.

### 6. File Upload Buffering Limits
- `AutoFormBase` must enforce a safe default file size limit (50 MB) and file count limit (100) when DTO metadata does not specify custom limits.

---

## Common Modification Scenarios

1. **Creating an AutoQuery Data Grid**
   ```razor
   <AutoQueryGrid Model="Booking" Apis="Apis.AutoQuery<QueryBookings, CreateBooking, UpdateBooking, DeleteBooking>()" />
   ```

2. **Customizing Form Inputs**
   - Use `[Input(Type = InputType.Combobox)]` or `[FieldCss]` on Request DTO properties.
