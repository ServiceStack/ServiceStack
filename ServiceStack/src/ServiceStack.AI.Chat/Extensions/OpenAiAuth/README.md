# ChatGPT subscription settings

`OpenAiAuthExtension` supplies the shared OpenAI Subscription settings panel. Host Identity remains the
login authority. Each grant, pending sign-in, model catalog and refresh operation belongs to that
request's authenticated username; unauthenticated installations use `default`.

The defaults follow the public Sign in with ChatGPT token-sharing protocol verified on 2026-10-05:
[sign-in](https://developers.openai.com/siwc/token-sharing-open-source/sign-in),
[token reference](https://developers.openai.com/siwc/token-sharing-open-source/token-reference), and
[models and inference](https://developers.openai.com/siwc/token-sharing-open-source/models-and-inference).
They deliberately replace the frozen Python extension's private Codex endpoint and static client ID.
The issued dynamic client ID, resource, nonce, verified OIDC subject and granted plan-use scope are
retained. Subscription inference uses public `/v1/responses`, streaming with `store=false`. Visible
account models come from the OAuth-authenticated model catalog in its original order; the frozen model
aliases are compatibility fallbacks, not promises of account entitlement. Endpoint and model defaults
are configurable through `OpenAiAuthExtension.Options`.

## Supported callback behavior

Automatic callbacks are enabled by default (`Options.AutomaticCallback = true`). For a local .NET app,
**Continue with ChatGPT** starts a loopback HTTP receiver before returning the authorization URL.
It prefers `http://127.0.0.1:1455/auth/callback`; if the port is occupied, it selects an available port.
Authorization and token exchange use the exact same selected URI. The receiver validates the pending
state to resolve the initiating user's partition, then applies the existing redirect, PKCE, client ID,
OIDC signature/nonce, account and scope checks. It activates the subscription provider, closes the
sign-in tab, and the settings panel observes the connection through status polling. No URL copying is
required. The listener binds only to `127.0.0.1`, does not log callback URLs, and stops with the host.

Callbacks cannot select a username from query parameters. Replayed, expired, disconnected or superseded
flows cannot exchange credentials. Sign-in errors appear in the initiating user's settings panel.
Pending flows expire after ten minutes. The optional `return_url` from the UI must belong to the same
origin and Chat route prefix; it supplies the return link/navigation if the browser cannot close the tab.

The loopback receiver requires the .NET process and browser to run on the same computer. For an eligible
hosted deployment with an OpenAI-registered HTTPS callback, configure `Options.RedirectUri` to
`https://your-host/chat/ext/openai_auth/callback` (adjust `/chat` to `ChatFeature.RoutePrefix`) and the
registered `Options.ClientId`. The protected host callback additionally verifies the current host identity
matches the sign-in initiator. A dynamic OSS registration does not support an arbitrary HTTPS redirect.
See OpenAI's [sign-in requirements](https://developers.openai.com/siwc/token-sharing-open-source/sign-in)
and [remote deployment guidance](https://developers.openai.com/siwc/token-sharing-open-source/self-hosted-vms).

Hosts explicitly choosing manual completion can set `Options.AutomaticCallback = false`. Only that mode
shows the callback URL entry in the shared UI; it requires the complete URL with `code`, `state` and
issued `client_id`. Bare codes are rejected. Controlled signed-issuer and real C# browser tests cover
both modes. Live account authorization and plan inference still require live verification; tests never
call paid inference or a live issuer.

## Credentials and import

Credentials are stored atomically as `user/{username}/credentials/openai_subscription.json` below
`ChatFeature.AppData`, with mode 0600 on Unix. `openai-agent-host.json` at the data root retains the
stable host identity. Protect both in backups and operate one host process per App_Data root. Restore
the same application's issued registration and host identity together; no migration reads the operator's
CLI profile automatically.

`has_codex_auth=false` by default and `import_codex` returns an explanatory unavailable response. A host
must explicitly set **both** `Options.LocalCredentialsPath` (an identity-specific trusted file resolver)
and `Options.CanImportLocalCredentials` (authorization policy) to enable imports. Only verified grants
with an issued application `client_id`, unexpired access token and plan-use scope are accepted. CLI
credentials are not assumed to be issued for this application.

Disconnect removes this user's local grant and pending flow. It does not confirm remote revocation;
revoke the application's access in ChatGPT account settings when needed. A late exchange/refresh cannot
undo disconnect. Refresh rotation is serialized across extension instances sharing the same user path.
Standard OpenAI API-key fallback remains available only when this user has no subscription grant (or
requests an API-key-only output modality). Expired or rejected grants never silently change billing.

## Streaming and verification

Responses text, reasoning, function calls and usage map to the existing chat contract without rewriting
canonical history. Request-local bearer/account headers never enter a shared provider. Only one refresh
retry is permitted for an HTTP 401. Incomplete, failed, cancelled or interrupted requests cannot be
replayed by the outer chat retry/failover loop. Subscription pricing is zero in local usage metadata.

From the downstream monorepo:

```sh
dotnet test ServiceStack/tests/ServiceStack.AiTests/ServiceStack.AiTests.csproj --filter FullyQualifiedName~AiChatMigrationOpenAiAuth
```

Before changing protocol defaults, check the official documentation and repeat controlled issuer,
refresh-race, Responses and browser tests. Keep the shared settings UI byte-identical to its reviewed
upstream source.
