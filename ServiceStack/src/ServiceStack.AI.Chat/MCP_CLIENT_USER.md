# Outbound MCP connections

`McpClientExtension` (`mcp_client`) consumes host-approved remote tools. It is disabled by default and
independent of the inbound [MCP server](MCP.md). Enabling the client does not re-export remote tools.
The Python port exposes the same routes and uses the same UI. Edit shared UI in `llms/extensions/mcp_client/ui` and copy it with `./sync.sh --extension mcp_client`.

## Connection configuration and UI

Connections live in extension-scoped files under the host's `App_Data/chat`:

- `user/default/mcp_client/config.json`: shared connections, managed by the host.
- `user/<username>/mcp_client/config.json`: personal connections, managed by that user in the UI.

Both use this shape:

```json
{
  "servers": [
    {
      "id": "knowledge",
      "displayName": "Company Knowledge",
      "endpoint": "https://knowledge.example.com/mcp",
      "auth": { "mode": "anonymous" },
      "allowedTools": ["search", "read_document"],
      "deniedTools": [],
      "includeInAll": false
    }
  ]
}
```

Enable the extension in the host:

```csharp
var chat = new ChatFeature();
chat.McpClient.Enabled = true;
Plugins.Add(chat);
```

Open **Tools → MCP Connections → Add connection** to create a personal connection. Enter **Name** and **Server URL**, choose authentication if needed, then click **Connect**. The UI generates the ID,
saves the connection, and discovers tools. New connections discover all tools by default; calls
still require approval and tools must be selected for a conversation. Search a connected server's tools and use **Select all results** to select the matching tools at once. OAuth
requires registered client details and host sign-in support; it is not automatically registered. Tool restrictions and include-in-all behavior remain available to hosts through configuration files. Each personal connection has
**Edit** and **Remove** actions; removing it also deletes its current saved credentials. Shared
connections are clearly labelled and cannot be edited through this UI. An authenticated account
named `default` cannot edit the shared file through the personal configuration API.

If an older personal configuration has an empty tool allow-list, its Tools panel explains why it is
empty and offers **Show discovered tools**. Saving that change may require signing in again; Bearer
connections must re-enter their token. Shared connection policy is unchanged.

Shared and personal connections are combined; duplicate IDs are rejected, never overridden.
Configuration is read on subsequent requests, so connection changes do not require a restart.
Saved edits invalidate old handles and pending OAuth callbacks; the connection must be signed in
again after an OAuth configuration change. Saves are atomic and use revision checks to reject stale
edits from another browser tab. Multi-instance deployments must share the configuration directory,
in addition to their database and Data Protection keys.

Personal connections support anonymous remote access, Bearer tokens, or user OAuth and **always require tool
approval**. They cannot specify shared-secret references, audience policies, approval bypasses,
limits, redirect URLs, or network exceptions. These remain host policy. OAuth tokens remain in
encrypted database storage, never in configuration JSON. Personal and shared configuration identities
are distinct, even if an ID is reused after a shared connection is removed.

For OAuth, set `auth.mode` to `user_oauth` and supply `oauthClientId`, `oauthIssuer`, and optional `oauthScopes`.
Providers such as GitHub also require an OAuth client secret. Enter it in the connection form;
the client stores it encrypted in the connection's credential record, not in `config.json`,
and sends it only to the discovered token endpoint. Clearing credentials deletes the
OAuth tokens but keeps the saved client secret so the account can authorize again. Removing
or reconfiguring a connection clears the client secret. Editing a connection's configuration may require
entering the secret again because credentials are bound to the configuration.
The host must configure `OAuthRedirectUri` and persistent Data Protection as described below.
For a shared service account, use `auth.mode: "host_secret"`, a `secretReference`, and an explicit
`allowedUsers` audience in the **default** file; register the host's `CredentialStore` in code.
Shared files also support `requiredRoles`, `approval`, `toolsWithoutApproval`, and `revision`.
Code-defined shared servers are still available for hosts needing custom authorization callbacks;
their IDs must not conflict with either file.

Files are limited to 64 KiB and 64 connections each. Unknown keys, null values, duplicate IDs,
and invalid policies are rejected. Paths are selected from the authenticated owner, not request input.
The configuration API is `GET /ext/mcp_client/config.json` (personal servers, revision, canEdit) and
`POST /ext/mcp_client/config.json` (`servers` and the last-read `revision`), with the existing
`X-Mcp-Client: 1` and same-origin mutation checks. Shared secret references are never returned by it.

There is no `mcpClient` section in `llms.json`. Extension enablement, limits, network policy,
secret resolution, OAuth callback URLs, and background identity authorization belong to host code.
These host settings require a restart; connection files are not overwritten by bundled config updates.

## Connecting and disabling connections

**Refresh connections** reloads the displayed status. **Connect all** connects every enabled
connection and refreshes its tools using saved credentials. Each failure is reported separately;
OAuth accounts requiring browser sign-in must be connected individually.

Use **Connection options → Disable** to exclude a connector from Connect all and tool discovery.
This choice is saved per user and survives restarts; credentials are retained. **Enable & connect**
restores it. A connection that has not connected yet is enabled by default.

## GitHub MCP with a personal access token

In **Tools → MCP Connections → Add connection**:

1. Set Name to `GitHub` and Server URL to `https://api.githubcopilot.com/mcp/`.
2. Under **Authentication**, select **Bearer token / personal access token**.
3. Paste the PAT into **Bearer token**, without the `Bearer ` prefix.
4. Click **Connect**, then **Tools** to search and select tools for your conversation.

The client sends `Authorization: Bearer <token>`. The token is submitted separately from configuration,
encrypted with the host's ASP.NET Core Data Protection provider, and scoped to the current user and
connection configuration. Register Data Protection with persistent keys shared between instances;
there is no plaintext fallback. `config.json` contains only `"auth": { "mode": "bearer" }`.
The token is never returned by the configuration API or prefilled in the browser. Leave the field blank
when editing to retain it; enter a replacement to rotate it. Changes to connection settings can invalidate
the saved token. **Connection options → Clear saved credentials** removes it. Disable retains it.
Tokens are not refreshed automatically; replace an expired or revoked PAT through **Settings**.

## Configure a service account

```csharp
var chat = new ChatFeature();
chat.McpClient.Enabled = true;
chat.McpClient.CredentialStore = mySecretStore; // implements IMcpClientCredentialStore
chat.McpClient.Servers.Add(new McpClientServer {
    Id = "knowledge",
    DisplayName = "Company Knowledge",
    Endpoint = new Uri("https://knowledge.example.com/mcp"),
    Auth = McpClientAuth.HostSecret("knowledge-token"),
    AllowedTools = ["search", "read_document"],
    RequiredRoles = ["Employee"],
    // Required for shared credentials: explicitly define the application's audience.
    Authorize = (context, operation) => Task.FromResult(context.User != null),
});
Plugins.Add(chat);
```

The credential store returns `McpClientCredential(accessToken, revision)`. Resolve secrets in the host's
secret manager; change `revision` whenever the credential changes. Tokens and credential references are
never returned to the browser. Code-defined shared connections are set before plugin installation.
Use `McpClientAuth.Anonymous()` only for a server that needs no remote authentication; local host
identity and policy checks still apply.

An empty `AllowedTools` list exposes no tools. `"*"` explicitly allows all discovered tools;
`DeniedTools` always wins. Tools require approval by default. Hosts can explicitly populate
`ToolsWithoutApproval` or choose `Approval = McpClientApproval.Never` for a reviewed connection.
Server-provided read-only hints never bypass approval.

`all` includes local tools only, unless the server sets `IncludeInAll = true`. Select `mcp_knowledge`
or an alias returned by the tool catalog to include remote tools. Aliases include a stable hash to
avoid normalization, case, and truncation collisions; never construct a remote name from an alias.

## Configure user OAuth

Register ASP.NET Core Data Protection with persistent keys, shared between application instances. The
host must provide a registered OAuth client and an expected issuer:

```csharp
chat.McpClient.OAuthRedirectUri = new Uri("https://chat.example.com/chat/ext/mcp_client/oauth/callback");
chat.McpClient.Servers.Add(new McpClientServer {
    Id = "work",
    Endpoint = new Uri("https://work.example.com/mcp"),
    Auth = McpClientAuth.UserOAuth(),
    OAuthClientId = "registered-public-pkce-client",
    OAuthIssuer = new Uri("https://identity.example.com"),
    OAuthScopes = ["tools.read"],
    AllowedTools = ["search_issues"],
});
```

Open **Tools → MCP Connections**, select **Connect / sign in**, then follow the sign-in link.
On success, the callback notifies the open Tools page, which refreshes and displays the connection's
tools, and the sign-in tab attempts to close. If the browser keeps it open, use **Close this tab**;
**Load tools** remains available on the Tools page if automatic refresh is unavailable.
The SDK owns PKCE, protected-resource discovery, issuer/state validation, resource binding, token
exchange and refresh. The adapter never starts a server-machine browser or loopback listener.
Encrypted credentials, one-time authorization callbacks, and revision/lease checks use `ChatDb` and
its configured `NamedConnection`. Callback delivery can reach another instance when database and
Data Protection keys are shared. The SDK retains the live PKCE exchange in the initiating process:
a restart during sign-in requires a new sign-in, rather than resuming a partially completed exchange.
Expired authorization transactions are removed on subsequent sign-in creation.

For GitHub's hosted MCP server, register a GitHub OAuth App with the callback URL shown in the
connection form. In NorthwindAuto this is
`https://localhost:5001/chat/ext/mcp_client/oauth/callback`. Add a connection with server URL
`https://api.githubcopilot.com/mcp/`, select OAuth, and enter the OAuth App's client ID and
secret. Set the issuer to `https://github.com/login/oauth`; choose scopes appropriate to the
GitHub tools you need. Open sign-in and authorize the app in GitHub; the Tools page then loads the
connection's tools. A GitHub PAT uses the separate Bearer-token mode instead. GitHub does not
support dynamic client registration for its hosted MCP server, so the OAuth App must already exist.

Disconnect invalidates the local connection while retaining encrypted tokens. Delete credentials
also removes those local tokens. Remote token revocation is not performed; revoke access at the
remote identity provider when necessary. Hosts should call `DeleteUserCredentials(user)` from their
user-deletion workflow. Anonymous chat requires explicit `SingleUserMode = true` and the stable
`default` partition; OAuth and connection-mutation routes remain unavailable without host auth.

For durable agents, configure `ReauthorizeBackgroundRequest` to obtain a **current** host-authorized
`IRequest` for the persisted username. Returning null denies access. This callback must check current
account/tenant/role state; a username or serialized SDK client is not an authorization grant.

## Review and execute

Use the connections page to inspect schemas and include tools in the existing conversation selection.
When a tool requires approval, its MCP form shows the connection, schema, and editable JSON arguments.
The shared coordinator waits for the entire mixed API/MCP batch, and preserves edited arguments and
provider call IDs. Direct execution cannot bypass MCP approval.

An invocation is persisted before dispatch. Lost responses and interrupted claims become
`outcome_unknown`; they are not retried. Check the remote service, then explicitly continue without
replay in the approval form. An abandoned approval claim is classified after ten minutes, exceeding
the maximum call/drain deadline. Local bookkeeping cannot guarantee exactly-once remote effects.
A remote ServiceStack confirmation-token result is displayed as remote output, never automatically
redeemed by a local approval.

Text, structured JSON (including arrays/scalars), references, raster images and supported audio retain
provenance. References are not fetched. Media uses bounded data URLs in the owned thread/result rather
than publishing private bytes to `~cache`; normal conversation retention governs that data. PNG,
JPEG, GIF, WebP, MPEG audio, WAV and Ogg signatures/MIME types are checked. Unsupported content is
reported explicitly. Model adapters still determine which media modalities they can consume.

## Network, schemas, limits and operations

HTTPS on port 443 with normal TLS validation is the default. Proxies, automatic redirects, ambient
cookies, and outbound trace/baggage propagation are disabled. DNS results are checked at socket
connection time, including OAuth requests. Explicitly allow an enterprise private destination through
`NetworkPolicy.AllowPrivateAddress`; nonstandard ports require `AllowedPorts`. `AllowHttp` is for
explicit development fixtures, not an implicit local-server exception.

Schemas use JsonSchema.Net 9.4.0, with no remote reference fetching. Local JSON Pointer references,
nested objects/arrays, nullable values and enums are supported. Recursive/dynamic references,
regular-expression constraints, and `$id` scopes are rejected until bounded handling is available.
Arguments are checked against the original schema, never a weakened provider projection. Selected
tools fail with `unsupported_model_schema` when Gemini's sanitizer would remove a constraint.

| Limit | Default |
| --- | --- |
| Connection/discovery deadline | 15 seconds |
| Call deadline | 60 seconds, also bounded by `Tools.ToolTimeout` |
| Active calls | 4 per principal/connection; 16 per server/process |
| Waiting calls | 32 per principal/connection |
| Principal partitions | 128; idle eviction after 15 minutes |
| Catalog freshness | 5 minutes |
| Catalog tools / pages | 256 / 100 |
| Schema / arguments | 64 KiB |
| Catalog / selected remote definitions | 1 MiB each |
| Aggregate encoded HTTP response | 4 MiB |
| JSON/schema nesting | 32 |
| Failed connection backoff | 3 seconds |

Clients are operation-scoped and asynchronously disposed, rather than pooled: catalog snapshots are
cached, but bearer headers and SDK state are recreated after authorization/credential checks. This
trades handshake overhead for simpler revocation and bounded ownership. OAuth operations additionally
hold a database credential lease, serializing refresh across instances. Process concurrency limits
are not a distributed quota. Finite SSE responses are buffered under the response cap; standalone
notification streams are disabled. Polling/manual refresh handles catalog changes.

Subscribe to `ServiceStack.AI.McpClient` as both an `ActivitySource` and `Meter` for call durations,
active counts and error categories. Payloads, users, credentials and endpoints are not metric tags.
There is no Admin Profiling bridge or exporter dependency. Remote downtime affects connection state,
not application readiness. Schema initialization is additive; new tables are included in extension
schema initialization/teardown.

## Compatibility and validation

| Area | Verified here |
| --- | --- |
| Framework builds | net8.0 and net10.0 |
| Official MCP Core SDK | 2.2.0; SDK types contained in `Extensions/McpClient` |
| Streamable HTTP | JSON and finite SSE fixture responses |
| GitHub remote MCP | Live initialization and full catalog validation with a fine-grained PAT; no tools executed. Schemas default to Draft 2020-12 and accept `x-mcp-header` annotations. |
| Protocol fixtures | 2025-06-18 initialize/session generation; 2026-07-28 per-request generation |
| Other SDK-supported revisions | 2024-11-05, 2025-03-26, 2025-11-25; not individually exercised |
| Unsupported interaction | JSON/SSE `input_required` continuations blocked before replay |
| Persistence | SQLite isolation, additive initialization, encrypted records, atomic callback/credential/invocation claims |
| OAuth fixture | Browser authorization URL, SDK PKCE exchange, canonical resource binding, encrypted token storage and callback replay rejection |
| Approval fixture | Owned execution route, edited arguments, duplicate approve, cross-user rejection, paused batch and cancellation |
| UI | Browser fixtures for connections, selection, schemas, approval edits and effective-argument display; module syntax check |
| Sync | Shared UI copied verbatim from Python; focused sync fixture verifies overwrite and stale-file cleanup |

The SDK itself automatically handles multi-round-trip continuation. A policy guard blocks outgoing
`inputResponses`/`requestState` and any second `tools/call` in an operation-scoped session, including an
SDK auth retry. Unsupported operations fail visibly rather than succeeding empty or replaying.

Live OAuth-provider interoperability, end-to-end durable-agent restart scenarios, non-SQLite dialects,
live-host browser/provider combinations, and performance/load certification remain release validation work.
Local stdio, legacy SSE transport, prompts/resources browsing, sampling, roots, elicitation, MCP Apps
and MCP Tasks are outside this release's implemented capability set. See the full acceptance checklist
in [MCP_CLIENT.md](MCP_CLIENT.md); this validation table does not claim those unchecked gates passed.
