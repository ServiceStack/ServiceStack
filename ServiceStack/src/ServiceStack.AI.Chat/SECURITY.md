# Security Architecture & Protection in `ChatFeature`

This document details how `ChatFeature` protects endpoints against unauthorized usage, enforces role-based access control, and executes Model Context Protocol (MCP) API endpoints within an identity context.

---

## 1. Security Architecture Overview

All incoming Chat UI and extension API requests under `ChatFeature.RoutePrefix` are dispatched through `ChatHttpHandler`, which handles request lifecycle setup, authentication resolution, route execution, and centralized error handling.

Authentication and authorization logic is encapsulated by `IChatAuth` (default implementation: `IdentityChatAuth`), which bridges ASP.NET Core Identity claims principals and ServiceStack API Keys.

```
[ Incoming Request ]
        │
        ▼
┌─────────────────────────┐
│     ChatHttpHandler     │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│ ChatFeature.OnRequest   │ ──► Resolves Bearer API Key onto IRequest
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│     Route Matching      │
└───────────┬─────────────┘
            │
  ┌─────────┴───────────────────────────────────┐
  ▼                                             ▼
[ Public / Static / Auth Route ]     [ Protected API Endpoint ]
  │                                             │
  ▼                                             ▼
Serve Asset / Handle Sign-In        AssertUserName() / CheckAuth()
                                                │
                                                ▼
                                    ┌───────────────────────┐
                                    │    IdentityChatAuth   │
                                    │ - Enforce RequireAuth │
                                    │ - Enforce RequiredRole│
                                    └───────────────────────┘
```

---

## 2. Authentication & Role Enforcement (`RequireAuth` and `RequiredRole`)

### Configuration Flags
- **`RequireAuth`** (`bool`, default `true`): Governs whether authentication is enforced across the feature.
- **`RequiredRole`** (`string?`, optional): Specifies a mandatory role or scope required to access non-exempt endpoints (e.g., `RequiredRole = "Admin"` or `RequiredRole = "Manager"`).

### Dual-Mode Authorization (`IdentityChatAuth`)

`IdentityChatAuth.HasRequiredRole(IRequest request)` is the single check enforcing `RequiredRole`, evaluated
against the same identity `GetUserName()` resolves so authorization can't diverge from the identity acted on:

1. **Session / ClaimsPrincipal (Cookie Auth)**:
   - Evaluates `ClaimsPrincipal` via `request.GetClaimsPrincipal()`.
   - If `RequiredRole` is specified, requires `user.HasRole(RequiredRole)` (or `user.HasRole("Admin")`, following
     ServiceStack's convention that Admin satisfies any required role).

2. **API Key (Bearer Token Auth)**:
   - Evaluates `ApiKey` resolved onto `IRequest` via `IdentityChatAuth.ResolveApiKeyAsync(req)`.
   - If `RequiredRole` is specified, requires `apiKey.HasScope(RequiredRole)` (or the `Admin` scope).

3. **Unauthenticated Access**:
   - If both `ClaimsPrincipal` and `ApiKey` are missing when `RequiredRole` is set, the check fails.

An authenticated user without `RequiredRole` is treated as **anonymous**, not as a signed-in user:

- `GetUserName()` returns `null`, so `CheckAuth()` reports unauthenticated and every caller of either
  (`/v1/chat/completions`, `/upload`, all `/ext` routes via `AssertUserName()`) rejects the request.
- `GetAuthInfoAsync()` returns `null`, so `GET /auth` responds `401` and the UI keeps showing SignIn rather
  than treating the host's authenticated user as signed in to the Chat UI.
- `POST /auth/login` (credentials SignIn) responds `403 '{RequiredRole}' Role Required` and signs the rejected
  user back out, so a failed SignIn doesn't leave them holding the host's auth cookie.
- `AssertUserName()` throws `UnauthorizedAccessException("Authentication required: '{RequiredRole}' Role Required")`.

In `ChatHttpHandler`, any `UnauthorizedAccessException` is caught and converted into a `401 Unauthorized` JSON response (`ChatFeature.ErrorAuthRequired()`).

> `RequiredRole` gates authenticated access; it does not make the Exempt (Public) endpoints below private —
> those remain reachable by anonymous users exactly as they are when only `RequireAuth` is set.

---

## 3. Endpoint Protection Boundaries

Every route dispatched by `ChatHttpHandler` is **protected by default**. After matching a route it
applies one gate before invoking the handler:

```csharp
if (!match.Value.Route.AllowAnon && !feature.ChatAuth.CheckAuth(req).IsAuthenticated)
    return 401; // CheckAuth covers both RequireAuth and RequiredRole
```

A route is reachable anonymously only if it was registered with `allowAnon: true`
(`RouteRegistry`/`ExtensionContext.AddGet(..., allowAnon: true)`). An extension therefore cannot
leave an API open by forgetting its own `CheckAuth()` — a new route is protected unless it
deliberately opts out. `POST /v1/chat/completions` is a typed ServiceStack service rather than a
`ChatHttpHandler` route, so it enforces `CheckAuth()` in `ChatServices` directly.

### A. Exempt (Public) Endpoints
The complete `allowAnon` set — static UI assets plus what the SPA must reach to render SignIn:

| Endpoint Path | Purpose |
| :--- | :--- |
| `GET /ui/{path:.*}` | Serves static UI bundles (`ai.mjs`, `index.mjs`, CSS, assets). |
| `GET /custom/{path:.*}` | Serves custom embedded frontend assets. |
| `GET /ext/{name}/{path:.*}` | Each extension's static UI files (`AddStaticFiles()`). Registered after the extension's own routes, so it only serves paths its APIs didn't claim. The UI imports `/ext/credentials/index.mjs` to get the SignIn component itself. |
| `GET /themes/{theme}/ui/{file_name}` | Static theme assets, so SignIn renders themed. The `/themes` listing is protected. |
| `GET /favicon.ico` | Favicon handler. |
| `GET /chat/index.html` (SPA Fallback) | Unmatched routes render `index.html` to allow the SPA to mount and redirect to `SignInUrl`. |
| `GET /config` | Client configuration (`requiresAuth`, `authType`, `signInUrl`). Served **unfiltered** — the UI's chat modules read `config.defaults`/`status`/`extensions` at setup time even before SignIn, so this exposes provider ids, enabled/disabled state and default model names to anonymous callers. |
| `GET /ext` | Installed UI extension paths, so the SPA knows which modules to import. |
| `GET /models`, `/prefs` | `ai.init()` fetches these before it knows whether anyone is signed in. Answer with an **empty** `[]`/`{}` when unauthenticated rather than 401, preserving the shape the UI expects. |
| `GET /auth` | Current authentication state (`GetAuthInfoAsync`). `401` when unauthenticated or missing `RequiredRole`. |
| `POST /auth/login` | Credentials SignIn — necessarily anonymous. Enforces `RequiredRole` itself, returning `403` and signing the user back out. |
| `POST /auth/logout` | Signs out the current session. |

### B. Protected Endpoints
Everything else, enforced by the gate rather than per-handler checks — including `GET /status`,
`/providers`, `POST /providers/{provider}` (additionally `IsAdmin`), `POST /upload`,
`GET /~cache/{tail:.*}`, `GET /themes`, `/avatar/user`, `POST /transcribe`, `POST /mcp`, and every
extension API under `/ext/...` (App threads/requests, Agents, Skills, Publish, Gallery, Projects,
Gemini, Tools, CoreTools, SystemPrompts, Pdf, Analytics).

> Handlers still resolve `ctx.UserName` for data partitioning, and it is `null` for an anonymous
> request. Note that `ChatDb.GetThread`/`GetThreadColumn`/`DeleteThread`/`UpdateThreadStreamingMessage`
> **drop** the user predicate when it is null (unlike `ApplyUserFilter`, which maps null to the
> `default` partition). The gate is what prevents those from being reached anonymously.

---

## 4. MCP API Endpoints & Identity Context

### Endpoint Details
- **Path**: `POST {RoutePrefix}/mcp` (e.g. `/mcp` or `/chat/mcp`).
- **Transport**: Streamable JSON-RPC 2.0 over HTTP POST.

### Authentication & Authorization
`McpExtension.HandleAsync` inspects the request prior to processing JSON-RPC commands:
```csharp
if (Ctx.Feature.ChatAuth.IsEnabled && req.UserName == null)
    return Unauthorized();
```
When `RequireAuth` is enabled, unauthenticated MCP requests immediately return `401 Unauthorized` with a `WWW-Authenticate: Bearer` header.

### Execution Within Identity Context

**Yes, MCP API endpoints are executed strictly within an Identity Context.**

1. **Bearer Token Resolution**: MCP clients (such as Claude Code, Cursor, VS Code, or custom AI agents) authenticate using Bearer API Keys (`Authorization: Bearer <key>`). During `ChatFeature.OnRequestAsync`, `IdentityChatAuth.ResolveApiKeyAsync(req)` runs before route handling and attaches the validated `ApiKey` DTO to the request.
2. **User Context Propagation**: When an MCP client invokes a tool via `tools/call`, `McpExtension.CallToolAsync` constructs a `ChatContext`:
   ```csharp
   var context = new ChatContext { User = req.UserName, Request = req.Request };
   var (text, resources) = await Ctx.Feature.ExecToolAsync(name!, toolArgs, context).ConfigAwait();
   ```
3. **Identity Scoped Tool Execution**: `ExecToolAsync` passes `context` into the target tool implementation (such as `api_tools`, database operations, or file system operations). Consequently, tool actions run under `req.UserName` and inherit the specific permissions, role/scope constraints, and data isolation of the calling identity.
