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
preserves C#-owned `identity` and `credentials` UI directories. Read `sync.sh` before changing its copy,
skip, preserve, or deletion rules.

### Running the sync

From `ServiceStack.AI.Chat`:

```bash
./sync.sh
```

The default upstream package is `../../../../llms/llms`. An explicit package directory can be passed as
the first argument. The script uses `rsync --delete` for synchronized directories, so do not keep C#-only
files inside a synchronized directory unless the script explicitly preserves that directory.

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
- it adjusts the root navigation behavior in `index.mjs` for a prefixed mount.

URLs persisted by the application remain prefix-free; `RoutePrefix` is a deployment concern. If upstream
changes remove the exact source strings used by these transformations, the C# host logs a warning. Review
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

### MCP references (C#-only feature)

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
