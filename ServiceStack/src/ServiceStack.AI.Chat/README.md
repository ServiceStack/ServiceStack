# ServiceStack.AI.Chat

A C# port of [llms-py](https://github.com/ServiceStack/llms) **v4** — a self-hosted AI Assistant with an
OpenAI-compatible API — packaged as a ServiceStack plugin using **Identity Auth**, **OrmLite** persistence
and the host's **App_Data** folder.

The UI is copied verbatim from llms-py via [sync.sh](#syncsh), so the Chat UX stays identical across both
platforms; only the backend is re-implemented.

## Quick start

```csharp
// Register plugins before AddServiceStack so its async startup loader sees them.
services.AddPlugin(new ChatFeature());
services.AddServiceStack(typeof(MyServices).Assembly);
```

Provider API keys are read from environment variables (`GROQ_API_KEY`, `OPENAI_API_KEY`, `ANTHROPIC_API_KEY`,
`GEMINI_API_KEY`, …) exactly as llms-py does. Browse to `/chat`.

Requires an `IDbConnectionFactory` registered with OrmLite (any supported RDBMS) for chat history.

### Providers

All of llms-py's first-party providers are supported, including the two with custom wire formats —
**Anthropic** (Messages API: content blocks, typed streaming events, thinking, tool use) and
**Google Gemini** (generateContent: contents/parts, thinkingConfig, safety settings, and the restricted
JSON-schema subset its tool definitions require).

Image/audio generation is available on the providers that offer it (`openai`, `openrouter`, `fireworks-ai`,
`zai`, `chutes`, `nvidia`) plus Gemini's image models, along with OpenRouter text-to-speech and Mistral
(voxtral) transcription. Generated media is written to the content-addressed cache and automatically
appears in the gallery.

> Note: llms-py configures `anthropic` with `"npm": "@ai-sdk/anthropic-cli"`, which shells out to a local
> `claude` binary to reuse a Claude Code subscription. That doesn't suit a web host, so this port maps
> **both** `@ai-sdk/anthropic` and `@ai-sdk/anthropic-cli` to the API-key provider — `anthropic` works with
> `ANTHROPIC_API_KEY` out of the box.

### Configuration

```csharp
services.AddPlugin(new ChatFeature {
    RoutePrefix = "/chat",            // "" mounts the UI at the site root
    RequireAuth = true,               // false runs everything as the "default" user
    IncludeInGeneratedDtos = false,  // opt-in to generating ServiceStack.AI client DTOs
    AuthType = ChatAuthType.OAuth,    // OAuth = Identity Auth cookies, ApiKey = ApiKeysFeature
    SignInUrl = "/Account/Login",     // where the UI sends users to sign in
    AppDataPath = null,               // defaults to {ContentRoot}/App_Data/chat
    NamedConnection = null,           // use a separate OrmLite connection for chat data
    AutoInitSchema = true,
    EnableProviders = ["groq"],       // restrict to specific providers (default: all enabled in llms.json)
    DisableExtensions = [],
    Variables = {                     // resolved before environment variables
        ["GROQ_API_KEY"] = "...",
    },
    ToolsConfig = new() {             // server-side execution is OFF by default
        EnableCodeExecution = false,
        EnableFilesystemTools = false,
        AllowedDirectories = [],
    },
});
```

## Calling chat and decisions from code (C# only)

`ChatFeature` registers an `IChatClient` for in-process calls. `ChatAsync` runs a completion through
the full chat pipeline (provider failover, tools, filters, usage), the same as `POST /v1/chat/completions`.

`CreateDecisionAsync` calls OpenRouter's [Decisions API](https://openrouter.ai/docs/api/api-reference/alphadecisions/submit-a-decisions-questions-and-answers-request)
(e.g. TypeSafe's Jev decision model) through the configured `openrouter` provider and its API key. The
model answers narrow, typed questions about some state; your code owns the workflow:

```csharp
var decision = await chatClient.CreateDecisionAsync(new CreateDecision {
    Model = "~typesafe/jev-latest",   // the default
    State = "Help! My payouts have been failing for 3 days.",  // text, or an object/array of context
    Questions = {
        ["is_urgent"]   = DecisionQuestion.Noul("Does this message convey urgency?", "Explicitly time-sensitive", "No urgency expressed"),
        ["department"]  = DecisionQuestion.Choice("Which team should handle this?", new() {
            ["billing"] = "Payments, invoicing, refunds", ["technical"] = "Bugs, outages, integrations", ["sales"] = "Pricing, upgrades, new accounts" }),
        ["frustration"] = DecisionQuestion.Score("How frustrated is the customer?", "Calm", "Frustrated", "Very angry"),
    },
});
if (decision.Noul("is_urgent") > 0.8 && decision.Choice("department") == "billing") { /* escalate */ }
var distribution = decision.Answers["department"].Probabilities;
```

A decision is one paid request: unlike chat it is never retried or failed over, never stored in chat
history, and runs no tools or chat filters. Requests are validated before sending and answers are
validated against the questions. Failures throw `ChatDecisionException` (an `HttpError`; its
`ProviderStatus` keeps OpenRouter's status). `User` and `SessionId` are sent to OpenRouter only when you
set them. The same API is available over HTTP as `POST /v1/decisions`, with the same authentication as
`/v1/chat/completions`. Decision usage/cost is returned in `Usage` but not recorded in chat analytics.

## Auth

| `AuthType` | Sign-in flow |
|---|---|
| `OAuth` (default) | Redirects to `SignInUrl` (ASP.NET Identity). The authenticated username partitions all data. |
| `ApiKey` | The stock llms-py API-key form; requires `ApiKeysFeature`. |

`POST /v1/chat/completions` accepts either an Identity Auth cookie or a Bearer API key (when
`ApiKeysFeature` is registered), so programmatic clients keep working regardless of `AuthType`.

With `RequireAuth = false` there's no sign-in and everything is stored under the `default` user,
matching llms-py's behaviour when no auth extension is installed.

## Storage

Files live under `{ContentRoot}/App_Data/chat` (llms-py's `~/.llms`):

```
App_Data/chat/
  llms.json  providers.json  providers-extra.json   seeded from embedded resources on first run
  cache/<2ch>/<sha256>.<ext> (+ .info.json)         content-addressed upload/generated-media cache
  .agent/skills/                                    shared skills
  user/<username>/                                  per-user prefs, profiles, themes, skills, avatars
  user/<username>/projects/<folder>/                a project's working folder (created on save)
  user/<username>/jev/                              portable recipes, immutable history and journals
  user/<username>/credentials/openai_subscription.json  private subscription grant
  user/<username>/publish/config.json                    identity-specific publisher settings/grant
  openai-agent-host.json                            stable subscription host identity
```

Threads, per-request accounting and the media gallery are stored via OrmLite in `ChatThread`,
`ChatRequest` and `ChatMedia`, partitioned by a `user` column. Missing columns are added
automatically on startup. Back up the database and App_Data before upgrading; newer identity constraints
and journals require restoring the matching backup for rollback. See [migration notes](Extensions/MIGRATION.md).

## Extensions

Ported from llms-py's modular extensions (`ChatFeature.Extensions`); add your own by implementing
`IChatExtension`.

| Extension | Provides |
|---|---|
| `app` | Threads, queued completions, long-poll streaming, token/cost accounting, avatars, themes |
| `agents` | Agent profiles (chat/coder/planner), system prompts, per-profile actions |
| `system_prompts` | The system prompt library |
| `projects` | Ordered/archived projects, read-only explorer, durable owned folder/Git creation |
| `git` | Hosted-safe repository provisioning, status/history/diffs, reviewed commits and fast-forward sync |
| `jev` | Decision Studio recipes, recorded examples/history, imports, publisher sharing and stars |
| `openai_auth` | Per-user ChatGPT subscription settings, hosted manual PKCE/OIDC and Responses inference |
| `tools` | Tool listing + direct execution for the tools UI |
| `core_tools` | `calc`, `get_current_time`, and code execution (opt-in) |
| `computer` | Filesystem tools + `run_bash` (opt-in) |
| `gallery` | Catalogue of uploaded/generated media |
| `skills` | Anthropic-style skill packages |
| `voice` | Speech-to-text via any OpenAI-compatible transcription API, or a local CLI (self-disables when no backend is available) |
| `publish` | Publish threads/media/projects to a remote llms.py site |
| `gemini` | Gemini File Search stores for RAG (self-disables without a Gemini API key) |
| `analytics`, `katex`, `identity` | UI-only |

`credentials`, `github_auth` and `browser` are intentionally not ported — Identity Auth replaces the
first two, and browser automation doesn't apply to a web host.

### Voice input

The `voice` extension tries `voxtype`, `transcribe`, `api` and `voxtral-mini-latest` in order, using the
first available — override with the `LLMS_VOICE` environment variable, or set `LLMS_VOICE=""` to disable.
This matches llms-py, including the configuration below.

`api` posts the recording to any OpenAI-compatible `/v1/audio/transcriptions` endpoint and needs nothing
installed. With no configuration it uses the first provider API key it finds — `GROQ_API_KEY`
(`whisper-large-v3-turbo`), `OPENAI_API_KEY` (`whisper-1`) or `MISTRAL_API_KEY` (`voxtral-mini-latest`).

llms.py ships with `mistral` / `voxtral-mini-latest` configured in `defaults.voice`. If that
provider has no API key it **falls back** to any other provider that does, so the shipped default
never disables voice input for someone using a different provider — `--verbose` logs `[fallback]`
when that happens.

> **Audio format.** Browsers record `webm/opus`, which Groq and OpenAI accept but Mistral rejects
> with *"Audio input could not be decoded"*. The chat UI converts the recording to 16 kHz mono WAV
> before uploading, so every provider works with no extra software. If the browser can't do the
> conversion the server falls back to `ffmpeg` when it's installed, and otherwise sends the
> original — in which case use `groq` or `openai`, which decode `webm` directly.

Configure it with a `voice` section under `defaults` in `llms.json`:

```json
{
  "defaults": {
    "voice": {
      "provider": "groq",
      "model": "whisper-large-v3",
      "language": "en"
    }
  }
}
```

| Setting | Purpose |
| --- | --- |
| `provider` | `groq`, `openai` or `mistral` — selects the endpoint and default model |
| `model` | Model id |
| `url` | Full endpoint URL; set instead of `provider` to use any other server |
| `api_key` | API key. Prefer `$SOME_VAR` over a literal key |
| `language` | ISO-639-1 hint, e.g. `en`. Omit to auto-detect |
| `prompt` | Biasing prompt for names and jargon |

A local speech-to-text server (speaches, faster-whisper-server) needs no key:

```json
{
  "defaults": {
    "voice": {
      "url": "http://localhost:8001/v1/audio/transcriptions",
      "model": "Systran/faster-whisper-small"
    }
  }
}
```

Each setting is overridable by an environment variable that takes precedence over `llms.json`:
`LLMS_TRANSCRIBE_PROVIDER`, `LLMS_TRANSCRIBE_MODEL`, `LLMS_TRANSCRIBE_URL`, `LLMS_TRANSCRIBE_KEY`,
`LLMS_TRANSCRIBE_LANG`, `LLMS_TRANSCRIBE_PROMPT`.

`voxtype` and `transcribe` shell out to local CLIs and additionally require `ffmpeg`; `voxtype` needs a
graphical desktop session so it doesn't apply to a web host.

> The browser only exposes the microphone in a secure context — HTTPS, or `localhost`/`127.0.0.1`. Over
> plain HTTP to any other host the button won't appear at all, regardless of configuration.

### Server-side execution

`core_tools`' `run_python`/`run_javascript`/`run_typescript`/`run_csharp` and `computer`'s filesystem
tools and `run_bash` let the LLM execute code and read/write files on the server. Unlike llms-py — which
assumes a single-user localhost app — they are **disabled by default** and must be enabled explicitly:

```csharp
ToolsConfig = new() {
    EnableCodeExecution = true,
    EnableFilesystemTools = true,
    AllowedDirectories = ["/srv/workspace"],   // every path is validated against these
}
```

Code runs in a temp directory with a stripped environment and (on Linux/macOS) `ulimit` CPU/memory caps.
Treat enabling these as granting the model shell access to the host.

There is no central workspace. Each user works in their own `App_Data/chat/user/<user>/workspace`
(anonymous requests use the `default` user's), which is the baseline when no project is active.
`AllowedDirectories` is empty by default; any directories listed there are explicitly shared with every
user's tools, and only admins can browse them in the workspace explorer or Git view. Selecting a project
*replaces* the baseline with that project's folder alone, so the model can only touch
`App_Data/chat/user/<user>/projects/<folder>` for as long as it's active.

### Gemini File Search (RAG)

`gemini` manages [Gemini File Search stores](https://ai.google.dev/api/file-search). Documents can be
uploaded directly (including ZIP expansion) or imported repeatably from trusted server folders with
include/exclude globs, category derivation, metadata rules, dry-run plans, deletion rails and saved run
history. Stable `(store, source, sourceKey)` identity makes an unchanged re-import free and lets changed
content safely replace its previous Gemini copy only after the replacement is live.

Explorer exposes hierarchical categories and facets for document type, status, locale, product,
versions and tags. Metadata can be edited in bulk and deliberately re-indexed. Chats preserve the
current Explorer filters in Gemini's `metadata_filter`; streamed and non-streamed answers retain
per-message grounding metadata for inline citations and source links. Sync reconciles local/remote
state, while prune removes unreachable duplicate remote copies.

The extension resolves `$GOOGLE_API_KEY`, then `$GEMINI_API_KEY`, then the configured `google` provider
key, and self-disables when none is available. `$GEMINI_UPLOAD_MIME_TYPES` overrides declared MIME
types (default `"mdx:text/markdown,cshtml:text/html"`). Uploads use bounded concurrency and transient
failure backoff; tune them with `$GEMINI_UPLOAD_CONCURRENCY` (default `4`) and
`$GEMINI_UPLOAD_MAX_RETRIES` (default `4`). Set `$GEMINI_WRITE_ROLE` (or `gemini_write_role` in config)
to restrict corpus mutations to a role. Admins configure non-admin filesystem access under
`gemini.importRoots` in the deployment-wide `config.json` or in the Import UI.

File stores/documents are stored in `ChatFilestore` and `ChatDocument`; repeatable imports and their
history use `ChatSource` and `ChatSourceRun`. Existing SQLite document tables are transactionally
migrated from hash identity on startup.

#### Website Assistants

The **Assistants** tab publishes a filtered File Store as an isolated Shadow DOM chat widget. Each
named Assistant owns its visitor-facing identity, category/facet scope, private system prompt,
fallback and conversation notice, theme palette, launcher appearance, origin allowlist and rolling
per-client request limit. Publishing makes its File Store public and produces a stable embed:

Behavior presets are provided for Documentation Guide, Technical Troubleshooter, Customer Support,
Developer/API Assistant, Product Advisor, Onboarding Guide, and Policy and Procedures use cases.
Their editable specialist instructions are combined server-side with shared rules for retrieval,
grounding, conflicting documents, prompt-injection resistance, conversation context, fallback
handling and response formatting.

```html
<script src="https://chat.example.com/ext/gemini/public/assistants/widget.js?g=abc123" async></script>
```

The server applies all document filters and prompt behavior; neither is exposed in the generated
JavaScript or overridable by the host page. The host may select the theme, position, accent and
built-in icon with `data-theme`, `data-position`, `data-accent` and `data-icon`. Empty origin rules
allow any website; exact HTTP(S) origins and `https://*.example.com` wildcard subdomains restrict
chat requests. The panel can open only when initiated, automatically after page load, or when the
visitor reaches the bottom of the page; an optional Ctrl/Command+K shortcut opens it independently.
Regenerating the deployment ID invalidates old embeds immediately.

Visitor sessions and recent visible messages are cached in the browser, while authoritative
conversations, messages and resolved source citations are retained in `ChatAssistant`,
`ChatAssistantConversation` and `ChatAssistantMessage` for support review. Archiving disables public
access but retains that history; permanent deletion removes it. Set `$GEMINI_ASSISTANT_MODEL` to
override the default `gemini-flash-latest` model.

## sync.sh

Re-copies the UI and seed configs from a local llms-py checkout:

```bash
./sync.sh [path-to-llms/llms]     # defaults to ../../../../llms/llms
```

It copies `ui/**`, shared extension `ui/` folders, runtime `prompts`/`examples`/Jev `recipes`, app
themes, agent profiles and seed configs. Gemini comes from the packaged `extensions/gemini`, as do Git,
Jev and OpenAI auth. Python `credentials`, `github_auth` and `browser` are skipped; C# `identity` and
`credentials` UI are preserved.

Full sync updates only synchronized assets inside the library's `chat/` directory. It never writes
upstream, clears deployed/Northwind App_Data, copies host custom components, or changes C# Identity and
credentials UI. Unknown C# extension directories and `chat/custom` are preserved. Source and destination
symlinks in synchronized paths are rejected. Inputs are checked before any asset mutation; interrupted
or failed writes roll back on ordinary process exceptions. Run one sync at a time.

The script needs Python 3 (standard library only), available with the llms-py development checkout;
`rsync` is no longer required. Bash is just a launcher; Windows can run `python sync.py` directly.
UI files, styles, prompts, examples, recipes, themes, profiles and library seed configs are copied
verbatim. `ui/tailwind/` is development input and is excluded. Existing deployed `llms.json` is untouched.

```sh
./sync.sh                         # synchronize all shared assets
./sync.sh --check                 # read-only parity check; exits nonzero on differences
./sync.sh --dry-run               # list proposed copies/deletions
./sync.sh --core                  # optional core-only synchronization
./sync.sh --extension jev         # optional extension-only synchronization
```

`chat/shared-assets.json` records current synchronized paths and hashes, and is updated automatically.
The packaged-resource test reads that embedded manifest; normal UI updates do not require hand-editing
frozen test hashes. Changed bytes get fresh destination timestamps so ordinary incremental builds
refresh embedded resources. Unchanged assets retain their timestamps. Stale files are removed only
inside shared directories; an obsolete extension is removed only if a prior sync recorded ownership.

Shared files stay byte-identical. The host supplies prefix mappings in the SPA/import map and replaces
`const base = ''` in served `ai.mjs`. Persisted links stay prefix-free. Never hand-edit `chat/ui/**` or
replace the main chat selector. After sync, build and run `ServiceStack.AiTests` to verify packaging and
backend/browser contracts. Syncing copies assets; it does not implement newly introduced backend APIs.

## New extension deployment boundaries

Git defaults to approved public HTTPS hosts without operator Git credentials, hooks or filters. Local
credential use requires explicit `GitExtension.UseLocalCredentials`; host filesystem policies remain
in force. Repository writes use the shared submission coordinator and reject active captured runs.
One process must own each App_Data root; this coordinator is not a cross-process filesystem lease.

Jev uses portable JSON recipes/history and sends raw decisions only to the configured OpenRouter
provider. Sharing uses the current user's publisher client. It does not migrate legacy `jev.sqlite`
directly or port the public publishing server. See [Jev storage/import/rollback](Extensions/Jev/README.md).

OpenAI auth is separate from host Identity. Its default public SIWC dynamic registration and Responses
endpoints are configurable. It supports manual full callback URLs, not hosted loopback/automatic OAuth.
Operator CLI import is disabled unless the host supplies both an identity-specific resolver and an
authorization policy. Expired grants do not silently fall back to API billing; disconnect clears only
that user's local grant. See [subscription configuration and live validation limits](Extensions/OpenAiAuth/README.md).

Publisher settings are isolated per user; only the public base URL/HTTP policy can be inherited. Deploy
with HTTPS and protect local subscription/publisher grants and backups. Project/Gemini/Jev upgrade and
rollback procedures and verification commands are in [migration notes](Extensions/MIGRATION.md).

## Migrating from the previous (llms-py v2) release

This is a rewrite; the following v2 APIs were removed:

- `IChatStore` / `DbChatStore` / `PostgresChatStore` / `ChatCompletionLog` — replaced by the
  `ChatThread` / `ChatRequest` schema. Analytics now come from the `analytics` extension's UI
  reading `/ext/app/requests/summary`.
- `AdminChatServices` and the Admin UI component — superseded by the analytics extension.
- `OpenAiProviderBase` / `OpenAiProvider` / `GoogleProvider` / `OllamaProvider` — replaced by
  `ChatProvider` / `OpenAiCompatibleProvider` and the per-provider subclasses.
- The hard `ApiKeysFeature` requirement — API keys are now optional.

`ChatCompletion` and the OpenAI request/response DTOs are unchanged, so `/v1/chat/completions` clients
keep working.

Existing `projects.json` files are migrated in place on read: each project gains a `folder` (a
kebab-case slug of its name unless set), its `publish` becomes a path relative to that folder, and the
old `paths` array — along with the `$WORKSPACE`/`$TEMP` aliases — is dropped the next time the project
is saved. Projects that pointed at directories outside `App_Data/chat` no longer reach them; move the
files under `user/<user>/projects/<folder>/` (created for you on save) or `user/<user>/workspace/`, or
explicitly share them with every user's tools via `ToolsConfig.AllowedDirectories`.

## Outbound MCP tools (C# only)

Enable the separately disabled `ChatFeature.McpClient` extension to consume host-approved Streamable
HTTP servers. It supports contextual selection, host credentials, user OAuth, shared approvals and
remote results without exporting imported tools through the inbound MCP server. See
[MCP_CLIENT_USER.md](MCP_CLIENT_USER.md) for configuration, defaults, tested compatibility and the
remaining release-validation gates.
