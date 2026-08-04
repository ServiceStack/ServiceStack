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

`IdentityChatAuth.AssertUserName(IRequest request)` enforces access control based on the authentication mechanism used:

1. **Session / ClaimsPrincipal (Cookie Auth)**:
   - Evaluates `ClaimsPrincipal` via `request.GetClaimsPrincipal()`.
   - If `RequiredRole` is specified, checks `user.HasRole(feature.RequiredRole)`.
   - If the user lacks the role, throws `UnauthorizedAccessException("Authentication required: role '{RequiredRole}'")`.

2. **API Key (Bearer Token Auth)**:
   - Evaluates `ApiKey` resolved onto `IRequest` via `IdentityChatAuth.ResolveApiKeyAsync(req)`.
   - If `RequiredRole` is specified, checks `apiKey.HasScope(feature.RequiredRole)`.
   - If the key lacks the scope, throws `UnauthorizedAccessException("Authentication required: scope '{RequiredRole}'")`.

3. **Unauthenticated Access**:
   - If both `ClaimsPrincipal` and `ApiKey` are missing when `RequireAuth = true`, `AssertUserName` throws `UnauthorizedAccessException("Authentication required")`.

In `ChatHttpHandler`, any `UnauthorizedAccessException` is caught and converted into a `401 Unauthorized` JSON response (`ChatFeature.ErrorAuthRequired()`).

---

## 3. Endpoint Protection Boundaries

Endpoints registered in `ChatFeature` fall into two explicit categories: **Exempt (Public)** and **Protected**.

### A. Exempt (Public) Endpoints
The following endpoints do not require authentication because they serve static UI assets, application configuration necessary for bootstrapping the client frontend, or authentication flows required for signing in:

| Endpoint Path | Purpose |
| :--- | :--- |
| `GET /ui/{path:.*}` | Serves static UI bundles (`ai.mjs`, `index.mjs`, CSS, assets). |
| `GET /custom/{path:.*}` | Serves custom embedded frontend assets. |
| `GET /favicon.ico` | Favicon handler. |
| `GET /chat/index.html` (SPA Fallback) | Unmatched routes render `index.html` to allow the SPA to mount and redirect to `SignInUrl`. |
| `GET /config` | Returns client configuration (`requiresAuth`, `authType`, `signInUrl`, providers) so the UI knows auth requirements. |
| `GET /status`, `/models`, `/providers` | Metadata endpoints exposing active LLM models and provider statuses. |
| `GET /ext` | Exposes installed UI extension paths for client UI loading. |
| `GET /auth` | Returns current authentication state (`GetAuthInfoAsync`). Returns `401` if unauthenticated. |
| `POST /auth/logout` | Signs out the current session. |
| `POST /auth/login`, `/auth/register` | Credentials extension endpoints used by users to authenticate or sign up. |

### B. Protected Endpoints
All non-static, operational, data-access, and extension endpoints are strictly protected:

| Endpoint Path                    | Protection Mechanism                                                                                   |
|:---------------------------------|:-------------------------------------------------------------------------------------------------------|
| `POST /v1/chat/completions`      | Calls `CheckAuth()` & evaluates `RequiredRole`. Streams completion within authenticated user context.  |
| `POST /upload`                   | Enforces `CheckAuth()`, rejecting unauthenticated uploads.                                             |
| `GET /~cache/{tail:.*}`          | Scoped cache retrieval and attachment download handler.                                                |
| `GET /prefs`                     | Uses `ctx.UserName`, which asserts user identity via `AssertUserName()`.                               |
| `POST /providers/{provider}`     | Requires `ChatAuth.IsAdmin(ctx.Request)`, restricting provider configuration to Admin role/scope.      |
| Extension Endpoints (`/ext/...`) | Every extension route (e.g. PDF generation, System Prompts, Agents, Skills, Tools, Gemini, Voice, Projects, Analytics) invokes `req.AssertUserName()`, automatically applying `RequireAuth` and `RequiredRole` checks. |

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
