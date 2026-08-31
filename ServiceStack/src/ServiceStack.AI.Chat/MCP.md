
# ServiceStack AI.Chat MCP Server

The built-in MCP server exposes selected AI.Chat tools to external AI assistants such as OpenCode, Claude Code, Cursor, VS Code, and other clients supporting MCP Streamable HTTP. It allows those assistants to use the same ServiceStack tools and API Tools as the built-in AI.Chat UI without publishing a second integration service.

For ServiceStack API discovery and calling behavior, read [API_TOOLS.md](API_TOOLS.md).

## Purpose

AI.Chat extensions register tools once in a shared `ToolRegistry`. The MCP extension projects an explicitly selected subset of those tools into MCP definitions:

- OpenAI-style function parameters become MCP `inputSchema`.
- Structured tool result schemas become MCP `outputSchema`.
- Tool safety becomes MCP annotations.
- Tool results become MCP text, structured content, media, or resource links.

This keeps the built-in chat UI and external assistants on the same executable tool implementations, authorization context, and schemas.

## Enabling MCP

Nothing is exposed by default. Select tool groups and/or individual tools explicitly:

```csharp
services.AddPlugin(new ChatFeature
{
    Tools =
    {
        EnableApiTools = true,
    },
    ApiTools =
    {
        IncludeTags = ["CoffeeShop"],
    },
    Mcp =
    {
        ToolGroups = ["api_tools", "bookings"],
        Tools = ["another_specific_tool"],
        ServerName = "coffee-shop",
        Instructions = "Use API Tools to inspect the current menu before placing an order.",
    },
});
```

`ToolGroups = ["all"]` or `Tools = ["all"]` exposes the entire registered tool set and should be used only when that broad access is intentional. Selecting an unexposed tool name in `tools/call` cannot reach it.

The default endpoint is:

```text
{scheme}://{host}/chat/mcp
```

The `/chat` prefix follows `ChatFeature.RoutePrefix`; changing the route prefix changes the MCP URL.

## Transport and protocol support

The server implements stateless MCP Streamable HTTP:

- Every MCP message is an HTTP `POST` containing JSON-RPC 2.0.
- Every request is self-contained.
- No SSE stream is offered.
- No `Mcp-Session-Id` is issued or required.
- `GET` and `DELETE` on the MCP endpoint return HTTP 405.
- Notifications are accepted without a JSON-RPC response.
- Legacy JSON-RPC batches are accepted for older negotiated protocol versions.

Supported MCP protocol versions are:

- `2025-06-18`
- `2025-03-26`
- `2024-11-05`

When the requested version is supported, the server returns it. Otherwise it responds with its latest version.

Supported methods are:

- `initialize`
- `ping`
- `tools/list`
- `tools/call`
- `notifications/*` as no-response notifications

The server advertises the `tools` capability with `listChanged: false`; tools are built at application startup and do not change during the process lifetime.

## Authentication

MCP routes use the same `ChatFeature` authentication gate as other protected AI.Chat routes.

When `ChatFeature.RequireAuth` is enabled, external clients should normally send a ServiceStack API key:

```http
Authorization: Bearer ak-example-key
```

`IdentityChatAuth` resolves the API key onto the request. Tools execute as the API key's user and retain that user's roles and scopes. API Tools additionally enforce each API's authentication, API-key, role, permission, claim, and scope requirements during discovery and execution.

When `RequireAuth = false`, MCP calls use the unauthenticated/default AI.Chat identity. Use open access only when every exposed tool is safe for anonymous callers.

## External client configuration

The exact configuration file shape is client-specific. A typical remote MCP entry is:

```json
{
  "type": "remote",
  "url": "http://localhost:5000/chat/mcp",
  "oauth": false,
  "headers": {
    "Authorization": "Bearer ak-example-key"
  }
}
```

For OpenCode versions whose configuration maps server names directly under `mcp`:

```json
{
  "$schema": "https://opencode.ai/config.json",
  "mcp": {
    "northwind-coffeeshop": {
      "type": "remote",
      "url": "http://localhost:5000/chat/mcp",
      "enabled": true,
      "oauth": false,
      "headers": {
        "Authorization": "Bearer {env:NORTHWIND_API_KEY}"
      }
    }
  }
}
```

Prefer an environment variable or the client's secret store rather than committing an API key.

## Tool discovery

`tools/list` returns only tools selected by `Mcp.ToolGroups` and `Mcp.Tools`. Each MCP tool may contain:

```json
{
  "name": "api_search",
  "description": "Search the APIs of the App...",
  "inputSchema": {
    "type": "object",
    "properties": {}
  },
  "outputSchema": {
    "type": "object",
    "properties": {}
  },
  "annotations": {
    "readOnlyHint": true
  }
}
```

Safety annotations are mapped as follows:

| ServiceStack safety | MCP annotations |
| --- | --- |
| `ReadOnly` | `readOnlyHint: true` |
| `Write` | `readOnlyHint: false`, `destructiveHint: false` |
| `Destructive` | `readOnlyHint: false`, `destructiveHint: true` |
| `Auto` or unspecified custom tool | No annotation |

Annotations are hints to the client, not an authorization boundary. The server still controls which tools are exposed and which caller may execute them.

## Calling tools

A client calls a selected tool with standard MCP parameters:

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "api_call",
    "arguments": {
      "name": "GetCoffeeShopMenu",
      "args": {}
    }
  }
}
```

Only properties declared in the registered tool's input schema are passed to its handler. Unknown top-level MCP tool arguments are discarded. API-specific arguments inside `api_call.args` are validated separately by API Tools.

The same tool handler used by AI.Chat executes with a `ChatContext` containing the authenticated username and HTTP request.

## Structured results

MCP always returns a `content` array. Text results are represented as:

```json
{
  "content": [
    {
      "type": "text",
      "text": "{\"status\":\"success\"}"
    }
  ],
  "structuredContent": {
    "status": "success"
  }
}
```

When a tool's textual result is a JSON object, the server also supplies `structuredContent`. Tools with an `outputSchema` should return structured JSON objects so strict MCP clients can validate the result.

API Tools expose output schemas and return structured objects for successful search, describe, and call operations.

## Images, audio, files, and resource links

Tool result media is converted to MCP content:

- Cached images and audio up to `MaxInlineResourceBytes` are returned inline as base64 `image` or `audio` content.
- Larger media, non-cached files, and other resources are returned as absolute `resource_link` entries.
- The default inline limit is 4 MiB.

Configure the limit when necessary:

```csharp
Mcp =
{
    MaxInlineResourceBytes = 8 * 1024 * 1024,
}
```

Avoid returning large media or datasets through a model context when a compact summary or resource link is sufficient.

## Approval policy

AI.Chat can pause a thread and display its editable schema-generated approval form. A generic MCP client cannot display or complete that ServiceStack UI flow. `ApprovalMode` defines how the MCP boundary handles mutating operations (`Write` or `Destructive` safety, or APIs marked `RequiresApproval = true`).

### Default: Two-Phase Confirmation Token

```csharp
Mcp =
{
    ToolGroups = ["api_tools"],
    ApprovalMode = McpApprovalMode.ConfirmationToken, // Default
    ConfirmationTokenExpiry = TimeSpan.FromMinutes(5),
}
```

This is the default mode. When a mutating operation is called without a token:
1. The server returns a `requires_confirmation` status containing a summary, argument parameters, and a signed, short-lived `confirmationToken`.
2. The AI assistant presents the summary to the user in chat for confirmation.
3. Upon approval, the assistant re-invokes `api_call` with the `confirmationToken`.
4. The server cryptographically validates the token (user identity, target API, payload argument hash, expiry, and single-use replay check) before executing.

Read-only operations (`IGet`, `QueryBase`, `QueryDb<>`, `QueryData<>`) execute immediately without requiring a token.

#### Production deployment

Two-Phase Confirmation is a security primitive; its cross-cluster correctness depends on **shared** state:

- **Signing secret must be shared and stable.** Configure `Mcp.SigningSecret` (or `HostConfig.AdminAuthSecret`) with a value of at least **32 bytes**, sourced from a secret store / env var. If neither is set, `McpExtension` generates an ephemeral per-process secret and logs a warning: tokens will not survive a restart and are rejected across load-balanced instances.
- **Register a distributed `ICacheClient`** (Redis via `IRedisClientsManager`, `OrmLiteCacheClient` on a shared DB, etc.) for the single-use replay set. Otherwise `McpExtension` falls back to an in-process `ConcurrentDictionary` and logs a warning: replay protection is per-process only and silently degrades in a farm. The used-token set uses TTL keys under `urn:mcp:used:{jti}` so it self-cleans.
- **`ApprovalHandler` must be deterministic.** The handler runs on both Phase 1 (mint) and Phase 2 (verify); its returned `Arguments` are hashed both times and compared. Any per-call mutation (attaching request IP, timestamps, correlation IDs, etc.) will break verification with `"Arguments have been modified since confirmation was issued"`. Side-effects such as audit logs also run twice per successful mutation.

### Fail-closed: Reject

```csharp
Mcp =
{
    ToolGroups = ["api_tools"],
    ApprovalMode = McpApprovalMode.Reject,
}
```

Before executing a tool, MCP runs its approval preflight. If the tool would require interactive approval, execution is refused with an approval-required error. Use this for strictly read-only exposure.

### Delegate confirmation to the MCP client

```csharp
Mcp =
{
    ToolGroups = ["api_tools"],
    ApprovalMode = McpApprovalMode.DelegateToClient,
}
```

MCP executes the tool immediately on the first call. The client is expected to use standard MCP safety annotations (`readOnlyHint: false`, `destructiveHint: true`) and its own native confirmation dialog to ask the user before mutating calls.

Use this mode only when:
- The MCP client is trusted.
- Its native confirmation policy is enabled.
- The API key is scoped to the intended user and capabilities.

## API Tools over MCP

Expose the `api_tools` group to give an external assistant:

- `api_search`
- `api_describe`
- `api_call`

Recommended assistant behavior remains search, describe, resolve prerequisites, preview, then call. For example:

1. Search for coffee ordering APIs.
2. Describe menu, preview, and create-order APIs.
3. Call the menu API to obtain current product IDs and choices.
4. Call the preview API to validate and price the order.
5. Call the create API.
6. Let the MCP client confirm the write when rejection is disabled.
7. Report success only from the returned structured result.

See [API_TOOLS.md](API_TOOLS.md) for the complete contracts and agent guidance.

## Custom AI.Chat tools over MCP

Any registered `ChatTool` can be exposed by group or name. Custom extensions can register a handler, output schema, safety, and optional approval preflight:

```csharp
ctx.RegisterTool(
    definition,
    handler,
    group: "reports",
    approvalHandler: null,
    outputSchema: reportOutputSchema,
    safety: ToolSafety.ReadOnly);
```

ServiceStack Commands registered through `ctx.RegisterTool<TCommand>()` can also be selected for MCP. Their Request DTO supplies the input schema and execution uses the same Commands infrastructure.

## Errors and troubleshooting

### HTTP 401

The API key is missing, invalid, or does not satisfy `ChatFeature.RequiredRole`. Confirm the `Authorization: Bearer` header and restart the client after changing configuration.

### HTTP 405

The client attempted `GET` SSE or session deletion. Configure it for MCP Streamable HTTP using JSON-RPC `POST`, not the older SSE transport.

### Tool is not available

The requested tool was not selected by `ToolGroups` or `Tools`, or its extension is disabled. Check the application's MCP configuration and restart it; the list is fixed at startup.

### API cannot be found

The API may not be opted in, may be excluded, or may be inaccessible to the API-key user. Use `api_search` rather than guessing its name.

### Requires interactive approval

The server has `ApprovalMode = McpApprovalMode.Reject`. Switch to `ConfirmationToken` (the default) to expose a two-phase approval flow to the assistant, or `DelegateToClient` when the MCP client's own confirmation dialog is trusted.

### Arguments have been modified since confirmation was issued

The Phase 2 argument hash does not match the token. Common causes:

- The AI assistant re-emitted the arguments with different numeric formatting or field ordering that `Canonicalize` doesn't handle (report this).
- The tool's `ApprovalHandler` is non-deterministic — it mutates `Arguments` differently on each call (see *Production deployment*).
- A shared signing secret is not configured and the token was validated by a different process than the one that minted it.

### Output schema but no structured content

A strict client received text that was not a JSON object from a tool advertising `outputSchema`. This can happen when an execution error is returned as plain text. Inspect server logs and the textual tool result; do not assume the operation succeeded.

### Connection succeeds but writes do not

Read/search/preview tools may work while the final write is rejected by approval policy, caller authorization, DTO validation, or service business rules. Treat each stage independently and preserve the actual error.

## Security checklist

- Expose only necessary tool groups or specific tool names.
- Prefer scoped, per-user API keys over application-wide credentials.
- Keep `RejectToolsRequiringApproval = true` unless client-side confirmation is intentional.
- Mark real side effects `Destructive`, even when implemented with `POST`.
- Do not rely on MCP annotations as authorization.
- Keep query and result limits small.
- Do not place API keys in committed configuration or screenshots.
- Rotate any key that has been disclosed.
- Audit service-side mutations independently of the assistant's narrative.

An MCP assistant is another API client. It should receive no more authority than the user it represents, and server-side authorization and validation remain the final enforcement boundary.
