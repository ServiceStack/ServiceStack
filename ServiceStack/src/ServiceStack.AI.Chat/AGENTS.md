# ServiceStack.AI.Chat implementation context

Read this file before changing `ServiceStack.AI.Chat`. It is the entry point for understanding the
relationship between this C# project and the upstream Python implementation, which files are generated,
which behaviors must remain compatible, and where intentional platform differences belong.

## Project purpose

`ServiceStack.AI.Chat` is the C#/.NET port of the ServiceStack
[`llms-py`](https://github.com/ServiceStack/llms) v4 self-hosted AI Assistant. It packages the same chat
application, provider behavior, extension model, and OpenAI-compatible API as a ServiceStack plugin.

The local projects are:

- C# port: `/home/mythz/src/ServiceStack/ServiceStack/ServiceStack/src/ServiceStack.AI.Chat`
- Python source and shared UI: `/home/mythz/src/ServiceStack/llms/llms`
- Synchronization script: `ServiceStack.AI.Chat/sync.sh`

Treat the two backends as implementations of the same product. Shared behavior should remain compatible
unless a difference is required by the .NET host, ServiceStack integration, or a deliberately C#-only
feature.

## Non-negotiable UI source-of-truth rule

The UI under `ServiceStack.AI.Chat/chat/` is synchronized from `llms-py`; it is not the primary editing
location.

For every shared UI change:

1. Edit the corresponding source in `/home/mythz/src/ServiceStack/llms/llms` first.
2. Implement and validate the behavior in the upstream UI.
3. Run `ServiceStack.AI.Chat/sync.sh` from the C# project directory.
4. Verify the upstream and copied files are byte-identical where they are expected to be identical.
5. Inspect both repositories' diffs so the sync does not introduce unrelated drift.

Examples:

- `chat/ui/**` comes from `/home/mythz/src/ServiceStack/llms/llms/ui/**`.
- `chat/ext/<extension>/**` normally comes from
  `/home/mythz/src/ServiceStack/llms/llms/extensions/<extension>/ui/**`.
- `chat/ext/<extension>/prompts/**` and `examples/**` are also copied when present upstream.
- `chat/themes/**`, `chat/profiles/**`, `chat/index.html`, `chat/llms.json`, `chat/providers.json`, and
  `chat/providers-extra.json` are synchronized by `sync.sh`.

Do not make a shared UI fix only in `ServiceStack.AI.Chat/chat/`: the next sync will overwrite it and the
Python and C# products will diverge. If a C#-specific UI difference is unavoidable, implement it as an
explicit C# integration point rather than silently forking a synchronized file.

The sync intentionally skips Python-only `credentials`, `github_auth`, and `browser` extensions and
preserves C#-owned `identity` and `credentials` UI directories. The `mcp_client` UI is shared with Python and copied verbatim. Read `sync.sh` before changing its copy,
skip, preserve, or deletion rules.

### Running the sync

From `ServiceStack.AI.Chat`:

```bash
./sync.sh
```

The default upstream package is `../../../../llms/llms`. An explicit package directory can be passed as
the first argument. The script uses Python 3 to copy shared bytes and remove stale files only within synchronized
directories. Keep C#-only assets in `chat/custom` or C# extension directories, not inside shared roots.
Full sync never writes upstream or host App_Data/custom components. `--check` verifies source parity
without mutation; `--dry-run` lists effects. The generated `chat/shared-assets.json` drives packaged
resource checks and records extension ownership for safe obsolete-extension cleanup.

After syncing, at minimum:

```bash
git diff --check
git status --short
cmp /home/mythz/src/ServiceStack/llms/llms/ui/ai.mjs chat/ui/ai.mjs
```

Use equivalent `cmp` checks for the files changed. Run relevant JavaScript checks and backend tests as
appropriate.

## `ai.mjs` is the shared UI adaptation point

[`chat/ui/ai.mjs`](chat/ui/ai.mjs) is copied verbatim from upstream and exposes the shared client-side API
used by core UI and extension UIs. Prefer adding deliberate, generally useful customization hooks there
when the Python and C# hosts need to adapt the same UI to different deployment environments or use cases.
Keep its public behavior compatible for every UI consumer.

The C# project does not maintain a hand-edited fork of `ai.mjs`. `ChatFeatureRoutes.TransformUiFile()`
applies deployment-specific transformations while serving synchronized files:

- it changes `const base = ''` in `ai.mjs` to the configured `ChatFeature.RoutePrefix`;
- it enables `staticPublishUseCurrentOrigin`, so an omitted static publication BaseUrl uses the browser origin;
- shared `index.mjs` now resolves startup navigation through `ai.resolvePath()`, so it needs no source rewrite.

URLs persisted by the application remain prefix-free; `RoutePrefix` is a deployment concern. If upstream
changes remove the exact source string used by the `ai.mjs` transformation, the C# host logs a warning. Review
these transforms whenever changing UI routing, `ai.base`, or SPA initialization.

## Backend parity rule

Shared C# and Python features should agree on observable contracts:

- routes, HTTP methods, request and response JSON;
- streaming events, tool-call behavior, errors, and status codes;
- configuration keys, defaults, environment-variable resolution, and provider selection;
- provider request translation and streamed/non-streamed responses;
- extension names, UI metadata, and capability discovery;
- thread, message, media, project, skill, and agent semantics;
- Gemini ingestion, metadata, assistant, search, analytics, and public-widget behavior;
- security boundaries and user partitioning, allowing for the intentional auth differences below.

When implementing a shared backend change, inspect the corresponding Python extension and its tests first,
then port the contract to C#. If the change originates in C#, update the Python implementation as well when
it is a shared feature. Add or update tests on both sides. Do not obtain apparent parity by copying Python
storage assumptions into C# when OrmLite or Identity requires a different internal design; preserve the
external behavior instead.

Useful implementation mappings:

| Concern | Python | C# |
| --- | --- | --- |
| App startup, routes, configuration | `llms/main.py`, `llms/llms.py` | `ChatFeature*.cs`, `Hosting/**`, `Configuration/**` |
| Persistence | `llms/db.py`, extension `db.py` files | `Db/**`, extension database classes |
| Providers | `extensions/providers/*.py` | `Providers/**` |
| Extension implementation | `extensions/<name>/*.py` | `Extensions/<Name>/**` |
| Shared extension UI | `extensions/<name>/ui/**` | generated `chat/ext/<name>/**` |
| Core UI | `ui/**` | generated `chat/ui/**` |

The implementation languages and persistence mechanics can differ. Route contracts and user-visible
semantics should not drift accidentally. Document intentional deviations in code comments and here when
they are architectural.

## Intentional C# differences

### Identity Auth and credentials

The Python application is primarily a single-user/self-hosted application with its own optional auth
extensions. `ServiceStack.AI.Chat` integrates into a host ServiceStack application:

- `IChatAuth` and `IdentityChatAuth` bridge ASP.NET Core Identity claims, ServiceStack Auth, cookies, API
  keys, `RequireAuth`, and `RequiredRole`.
- `ChatAuthType.OAuth` redirects to the host application's Identity login page.
- `ChatAuthType.ApiKey` uses ServiceStack's `ApiKeysFeature` flow.
- `ChatAuthType.Credentials` renders an in-chat login but authenticates against the host's ServiceStack
  credentials provider and ASP.NET Identity users.
- [`Extensions/Credentials/CredentialsExtension.cs`](Extensions/Credentials/CredentialsExtension.cs) is
  therefore not a port of Python's users-file/session implementation. User management remains the host's
  responsibility, normally through Identity/Admin Users.

All stored C# chat data is partitioned by the authenticated username. With `RequireAuth = false`, it uses
the `default` user for compatibility with an unauthenticated Python installation.

### OrmLite and multiple RDBMS backends

Python storage is SQLite-oriented. The C# port persists application data through the host's registered
OrmLite `IDbConnectionFactory`, optionally using `ChatFeature.NamedConnection`. Schema creation and
migration must remain additive and work across supported OrmLite databases; do not introduce raw SQL into
shared persistence paths without a dialect boundary and fallback.

The Gemini extension is an important example. `GeminiSearchDbProvider` owns database-specific full-text
schema and query generation for:

- SQLite FTS5;
- PostgreSQL `tsvector`/GIN full-text search;
- MySQL and MariaDB `FULLTEXT` search;
- SQL Server Full-Text Search when the optional component is installed.

It falls back to portable `LIKE` search when native FTS is unavailable or fails. Metadata array filters
also use database-specific JSON functions behind this provider. Preserve native-search and fallback
behavior when modifying Gemini indexing or search; test more than SQLite when changing dialect SQL.

### C#-only MCP server

[`Extensions/Mcp`](Extensions/Mcp) implements a C#-only MCP Streamable HTTP server that `llms-py` does not
provide. It exposes an explicitly selected subset of the shared `ToolRegistry` to external assistants,
retains ServiceStack identity/authorization context, supports structured and media results, and has a
server-enforced two-phase confirmation-token flow for unsafe operations.

Changes to tools, schemas, safety annotations, approvals, request identity, or API Tools must consider both
the built-in Chat UI pipeline and the MCP projection. The two approval systems are deliberately separate:
the browser UI can pause a thread for its rich approval form, while external MCP clients use signed,
short-lived confirmation tokens.

### Other host-oriented differences

- The C# port stores files below the host application's `App_Data/chat` instead of `~/.llms`.
- Server-side code and filesystem tools are disabled by default because this is a multi-user web host;
  enabling them must remain explicit and sandboxed by configured/project directories.
- The upstream Anthropic CLI provider shells out to `claude`; C# maps both Anthropic provider identifiers
  to the API-key implementation because a web host should not reuse a local CLI subscription.
- C# includes ServiceStack API Tools, PDF runtime integration, Admin UI integration, and host-specific
  configuration surfaces. Determine whether a feature is shared or C#-only before changing upstream.
- `IChatClient.CreateDecisionAsync` and `POST /v1/decisions` (`CreateDecision.cs`,
  `Providers/ChatDecisions.cs`) are a C#-only typed API over OpenRouter's Decisions API. They bypass the
  chat pipeline (never retried, failed over or stored). Jev's `DecisionClient` uses the same
  `ChatDecisions` transport, so keep its bounds, error mapping and no-replay rule shared; llms-py's
  `extensions/jev/client.py` remains the reference for request/response behavior.
- Project chat threads (upstream `docs/CHAT_THREADS.md`) share the same routes and contracts, with
  these implementation differences:
  - a durable run's captured workspace is applied through `WorkspaceScope` (an `AsyncLocal`, the
    equivalent of Python's `ContextVar`), which `ResolveAllowedDirectories` consults;
  - there is no central workspace: each user has `App_Data/chat/user/<user>/workspace`
    (`ChatFeature.GetUserWorkspace`; unauthenticated requests use `default`'s). A thread without a
    project uses that workspace plus any explicitly host-shared `ToolsConfig.AllowedDirectories`
    (empty by default), where Python grants no directories. The workspace explorer and Git show a
    user only their own workspace; host-shared directories are browsable by admins only, and
    Gemini import roots and local-file message references use only the requesting user's directories;
  - `projects.json` read-modify-write is serialized with an in-process lock plus atomic replace,
    not Python's cross-process file lock, because one web host owns `App_Data`;
  - `run_bash` already starts a fresh shell per command in the first allowed directory, so there
    is no per-run persistent shell to release;
  - sidebar and compare-and-set SQL in `Db/ChatDb.Sidebar.cs` is built from dialect-quoted
    identifiers and parameters (subqueries use `UnsafeAnd`) so it stays portable across OrmLite
    databases.

### Migration additions (2026-10-05)

- `Extensions/Git` supplies hosted-safe provisioning and reviewed repository operations; credentials,
  process execution and filesystem authority come from explicit host policy. Active captured agent
  workspaces block writes. `WorkspaceOperations` coordinates submissions and mutations process-wide
  for the normalized App_Data root; one process owns that root.
- `Extensions/Jev` implements restricted Decision Studio recipes, file-backed journals/history,
  recorded examples, raw decision execution and immutable publisher retries. It intentionally does
  not read legacy `jev.sqlite`. Read [Jev README](Extensions/Jev/README.md) before storage/sharing changes.
- `Extensions/OpenAiAuth` supplies per-identity grants and pending flows, atomic rotation, hosted manual
  callbacks and public Responses inference. It never borrows operator credentials implicitly or places
  bearer/account headers on shared providers. Read [OpenAI auth README](Extensions/OpenAiAuth/README.md)
  for protocol defaults, imports and unrun live OAuth limits.
- `ChatProviderRequestException` prevents the orchestrator from replaying/failing over a provider
  request with an uncertain outcome. Keep the bounded 401 refresh retry inside the subscription
  transport. Subscription model display names resolve against the originating user's catalog.
- `Extensions/ShareStatic` exports projects independently of remote sharing. `ShareStaticExtension.StaticPublish`
  accepts a typed `StaticPublishConfig` override; otherwise settings deserialize from
  `user/default/share_static/config.json` (enabled, web content directory/p, /p/, empty baseUrl defaults).
  An omitted Directory resolves under the host's web root; an explicit relative Directory uses the working
  directory. Empty BaseUrl uses the UI's current origin through ai.mjs's resolveStaticPublishUrl hook;
  ChatFeatureRoutes enables that fallback at serve time, including a root-mounted UI.
  Exports rewrite only copied HTML; metadata is separate and server-owned, using an in-process semaphore.
- `Extensions/ShareLlmspy` owns the bounded `PublisherClient` and per-user grants, with legacy publish
  grants migrated on save and removed on disconnect. Capture immutable origin/account before network I/O.
  Core `ctx.setShareOptions` owns ordered sharing tabs and the optional share icon; neither extension
  depends on the other. Edit upstream UI first and sync it with `sync.sh`.
- Projects archive/order/title changes preserve canonical history and `lastActivityAt`; drafts stay
  browser-only. Submission leases precede queue/approval writes. Gemini saved imports reserve exact
  owner/source/physical-manifest/key identities portably; do not revert to null-source adoption.

The main chat selector (`chat/ui/modules/model-selector.mjs`) remains unchanged. Shared `ModelPicker`
is for its existing extension uses. Do not stack focus indicators or change dimensions on focus.
Full `sync.sh`, as well as `--core` and `--extension NAME`, preserves upstream files, host App_Data,
C# identity/credentials/custom UI and unknown C# extensions. The script includes runtime `prompts`,
`examples` and Jev `recipes`; Gemini is packaged, not LLMS_HOME. The generated shared-assets manifest
tracks current source hashes; changed bytes receive fresh build-visible timestamps. Verify packaging
and browser/backend contracts with `ServiceStack.AiTests` after synchronization. See
[migration and rollback notes](Extensions/MIGRATION.md) for storage constraints and validation commands.

## Architecture orientation

- `ChatFeature.cs` configures the plugin, providers, extensions, auth, persistence, and host services.
- `ChatFeatureRoutes.cs` serves the SPA/static assets and core routes, including serve-time UI adaptation.
- `Hosting/**` supplies route registration, request context, JSON, HTTP dispatch, and extension hosting.
- `Pipeline/**` owns orchestration, messages, model prompts, tools, and approvals.
- `Providers/**` adapts OpenAI-compatible, Anthropic, Google, and media provider protocols.
- `Db/**` contains shared OrmLite tables and durable persistence.
- `Extensions/**` contains modular backend features; start with the extension entry class, then its routes,
  database code, and matching upstream Python extension.
- `chat/**` contains embedded application assets, mostly generated by `sync.sh`.

## Documentation map

The following list covers every `*.md` file currently under `ServiceStack.AI.Chat`. Check it again with
`rg --files ServiceStack.AI.Chat -g '*.md'` because synchronized extensions may add or remove documents.

### Project and integration references

- [`AGENTS.md`](AGENTS.md) — this entry point. Read first for source ownership, parity rules,
  deliberate platform differences, and directions to deeper documentation.
- [`README.md`](README.md) — installation and broad product reference: providers, auth modes, storage,
  extensions, voice, server-side execution, Gemini RAG/widgets, synchronization, and v2 migration. Read
  when configuring `ChatFeature` or learning the overall supported feature set.
- [`SECURITY.md`](SECURITY.md) — request dispatch, authentication, authorization, protected route
  boundaries, roles, and MCP identity context. Read before changing auth, route exposure, public endpoints,
  role enforcement, or request handling.
- [`API_TOOLS.md`](API_TOOLS.md) — design and usage of `api_search`, `api_describe`, and `api_call`, including
  opt-in API selection, schemas, identity, safety, approvals, and result limits. Read before changing
  ServiceStack API discovery/execution or teaching an assistant to call application APIs.

### MCP references (shared outbound client; C#-only inbound server)

- [`MCP_CLIENT.md`](MCP_CLIENT.md) — implementation plan for the separate `mcp_client`
  extension: outbound connections, contextual tools, OAuth, approvals, durable execution, and UI ownership.
  This is the implementation/release checklist, distinct from the existing inbound MCP server. Its UI now comes from upstream `extensions/mcp_client/ui`; use `./sync.sh --extension mcp_client` for a focused sync.
- [`MCP_CLIENT_USER.md`](MCP_CLIENT_USER.md) — outbound MCP client configuration, OAuth host requirements,
  contextual selection, approvals, network policy, limits, tested compatibility and remaining release validation.
- [`MCP.md`](MCP.md) — authoritative developer reference for enabling and exposing AI.Chat/API tools over
  MCP Streamable HTTP, authentication, tool/result mapping, approvals, errors, and security. Read before
  changing `Extensions/Mcp` or configuring an MCP server.
- [`MCP_USER.md`](MCP_USER.md) — end-user setup guide for connecting OpenCode, Claude Code, Cursor, and
  other MCP clients, with configuration and troubleshooting examples. Read when documenting or testing a
  client connection.
- [`MCP_CONFIRMATIONS.md`](MCP_CONFIRMATIONS.md) — architecture and threat model for the external MCP
  two-phase dry-run/confirmation-token protocol and its separation from built-in UI approvals. Read before
  changing unsafe-tool confirmation, token binding, expiry, replay protection, or approval schemas.

### PDF references

- [`PDF.md`](PDF.md) — comprehensive implementation contract for PDF Studio, Typst artifacts, publishing,
  Admin UI, code generation, rendering, security, deployment, validation, and supported scope. Read before
  modifying any C# PDF feature or public PDF contract.
- [`Extensions/Pdf/USER.md`](Extensions/Pdf/USER.md) — application-developer guide for consuming already
  published templates, generating typed C# models, returning PDFs, and attaching PDFs to email. Read when
  integrating `PdfFeature` into an application rather than modifying the designer.
- [`chat/ext/pdf/prompts/edit-template.md`](chat/ext/pdf/prompts/edit-template.md) — synchronized runtime
  system prompt governing AI edits to Typst templates, sidecar JSON, shared `lib.typ`, and attached-image
  recreation. Read before changing PDF Studio's AI editing behavior; edit upstream first.
- [`chat/ext/core_tools/prompts/generate-ui-schema.md`](chat/ext/core_tools/prompts/generate-ui-schema.md) —
  synchronized runtime prompt for generating Draft 2020-12 form schemas from PDF example JSON. Read before
  changing PDF form-schema generation; edit upstream first.

### Durable agent references

- [`Extensions/App/DURABLE_AGENTS.md`](Extensions/App/DURABLE_AGENTS.md) — C# durable-agent architecture:
  canonical message history, additive schema, run leases, execution slices, checkpoints, compaction,
  provider repair, UI retrieval, and operational boundaries. Read before changing agent scheduling,
  durable tables, message paging, streaming persistence, context compaction, or recovery.
- [`Extensions/App/USER_AGENTS.md`](Extensions/App/USER_AGENTS.md) — user-facing behavior of long-running
  agents, progress, refresh/recovery, paging, context reduction, cancellation, transport fallback, and
  configuration. Read when checking the intended UX or documenting durable execution.

### Synchronized skills and agent profiles

These files are embedded runtime instructions, not ordinary implementation documentation. Changes alter
model behavior and must be made in the corresponding upstream source before running `sync.sh`.

- [`chat/ext/skills/skills/create-plan/SKILL.md`](chat/ext/skills/skills/create-plan/SKILL.md) — concise,
  read-only coding-plan workflow and required plan format. Read when changing the bundled planning skill.
- [`chat/ext/skills/skills/skill-creator/SKILL.md`](chat/ext/skills/skills/skill-creator/SKILL.md) — detailed
  guidance for designing, structuring, validating, and packaging reusable assistant skills. Read before
  modifying the bundled skill-authoring workflow.
- [`chat/ext/skills/skills/skill-creator/references/output-patterns.md`](chat/ext/skills/skills/skill-creator/references/output-patterns.md)
  — reusable templates for reports, examples, and commit-message outputs used by skill authors.
- [`chat/ext/skills/skills/skill-creator/references/workflows.md`](chat/ext/skills/skills/skill-creator/references/workflows.md)
  — short patterns for sequential and conditional skill workflows.
- [`chat/profiles/chat/SYSTEM.md`](chat/profiles/chat/SYSTEM.md) — system instructions for the general chat
  profile. Read when changing default conversational-agent behavior.
- [`chat/profiles/coder/SYSTEM.md`](chat/profiles/coder/SYSTEM.md) — system instructions for the execution
  agent that implements an existing plan with strict completeness and validation. Read when changing coder
  profile behavior.
- [`chat/profiles/planner/SYSTEM.md`](chat/profiles/planner/SYSTEM.md) — system instructions for the planner
  agent that produces and saves structured implementation plans without executing them. Read when changing
  planner depth, plan format, risk analysis, or output rules.

### Vendored documentation

- [`chat/ext/katex/README.md`](chat/ext/katex/README.md) — upstream KaTeX package overview, browser setup,
  rendering API, contributors, and license. It is useful only when maintaining the bundled KaTeX assets or
  math-rendering integration; it is not an AI.Chat architecture document and should be updated upstream.

## Change checklist for AI assistants

Before editing:

- Read this file, `README.md`, and the domain-specific documents above.
- Decide whether the target is shared with `llms-py`, C#-only, or a serve-time adaptation.
- For shared UI, locate and edit the upstream file—not the generated copy.
- For shared backend behavior, compare Python routes, schemas, defaults, and tests with the C# port.
- For auth, persistence, FTS, MCP, PDF, or server tools, preserve the C# host's explicit security and
  multi-user boundaries.

Before finishing:

- Run `sync.sh` after upstream UI changes and inspect all resulting diffs.
- Verify copied assets match upstream.
- Run focused tests for both implementations when shared backend behavior changed.
- Run C# tests against relevant database dialects when persistence or Gemini FTS changed.
- Recheck public routes, authentication/authorization, error shapes, streaming, and cancellation where
  applicable.
- Update this context file or the deeper reference docs when adding a new architectural difference or
  Markdown document.

UI assets revalidate with content ETags and `Cache-Control: no-cache`. Buffered text/JSON responses
negotiate gzip or deflate; file downloads and SSE retain their streaming paths. Core module preloads
use the resolved, prefix-aware import map. CodeMirror is loaded by the shared UI on demand.
