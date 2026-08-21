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

`AllowedDirectories` is the baseline a user gets when no project is active. Selecting a project
*replaces* it with that project's folder alone, so the model can only touch
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
