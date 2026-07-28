# ServiceStack.AI.Chat

A C# port of [llms-py](https://github.com/ServiceStack/llms) **v4** — a self-hosted AI Assistant with an
OpenAI-compatible API — packaged as a ServiceStack plugin using **Identity Auth**, **OrmLite** persistence
and the host's **App_Data** folder.

The UI is copied verbatim from llms-py via [sync.sh](#syncsh), so the Chat UX stays identical across both
platforms; only the backend is re-implemented.

## Quick start

```csharp
services.AddPlugin(new ChatFeature());
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
```

Threads, per-request accounting and the media gallery are stored via OrmLite in `ChatThread`,
`ChatRequest` and `ChatMedia`, partitioned by a `user` column. Missing columns are added
automatically on startup, so upgrades don't need a manual migration.

## Extensions

Ported from llms-py's modular extensions (`ChatFeature.Extensions`); add your own by implementing
`IChatExtension`.

| Extension | Provides |
|---|---|
| `app` | Threads, queued completions, long-poll streaming, token/cost accounting, avatars, themes |
| `agents` | Agent profiles (chat/coder/planner), system prompts, per-profile actions |
| `system_prompts` | The system prompt library |
| `projects` | Per-user project folders the filesystem tools are sandboxed to |
| `tools` | Tool listing + direct execution for the tools UI |
| `core_tools` | `calc`, `get_current_time`, and code execution (opt-in) |
| `computer` | Filesystem tools + `run_bash` (opt-in) |
| `gallery` | Catalogue of uploaded/generated media |
| `skills` | Anthropic-style skill packages |
| `voice` | Speech-to-text (self-disables when no backend is available) |
| `publish` | Publish threads/media/projects to a remote llms.py site |
| `gemini` | Gemini File Search stores for RAG (self-disables without a Gemini API key) |
| `analytics`, `katex`, `identity` | UI-only |

`credentials`, `github_auth` and `browser` are intentionally not ported — Identity Auth replaces the
first two, and browser automation doesn't apply to a web host.

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

`AllowedDirectories` is the baseline a user gets when no project is active. Selecting a project
*replaces* it with that project's folder alone, so the model can only touch
`App_Data/chat/user/<user>/projects/<folder>` for as long as it's active.

### Gemini File Search (RAG)

`gemini` manages [Gemini File Search stores](https://ai.google.dev/api/file-search): documents uploaded
in the UI are deduplicated by SHA256, saved to the cache and uploaded to Gemini by a background worker,
then a chat can ground its answers on a store (or a single category/document) with an OpenAI-shaped
`file_search` tool that `GoogleProvider` forwards to Gemini. `POST /ext/gemini/filestores/{id}/sync`
reconciles the local catalogue with the store and records what didn't line up in each document's state.

It uses `$GEMINI_API_KEY` (falling back to the key the `google` provider resolved) and self-disables
when neither is configured. `$GEMINI_UPLOAD_MIME_TYPES` overrides the MIME type sent for specific
extensions, e.g. `"mdx:text/markdown,cshtml:text/html"` (the default). File stores and documents are
stored in the `ChatFilestore` and `ChatDocument` tables.

## sync.sh

Re-copies the UI and seed configs from a local llms-py checkout:

```bash
./sync.sh [path-to-llms/llms]     # defaults to ../../../../llms/llms
```

It copies `ui/**`, each ported extension's `ui/` folder into `chat/ext/<name>/`, the app themes, agent
profiles and `llms.json`/`providers.json`/`providers-extra.json`; it skips the unported extensions and
preserves the C#-only `chat/ext/identity/`.

`gemini` is a user extension rather than a packaged one, so its UI is synced from `$LLMS_HOME/extensions`
(default `<llms>/llms-home`, override with `LLMS_HOME=`) — add any further user extensions to `HOME_EXT`.

The synced files are used **as-is**. The single platform difference is applied when serving `ai.mjs`:
`const base = ''` becomes the configured `RoutePrefix`. Anything else the prefix affects is handled by
the UI itself, so a sync can't clobber it.

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
files under `user/<user>/projects/<folder>/` (created for you on save) or keep them available to every
user via `ToolsConfig.AllowedDirectories`.
