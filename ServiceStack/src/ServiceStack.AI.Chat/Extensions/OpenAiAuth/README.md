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

This implementation provides manual callbacks. Sign in, then use **Manual redirect entry** to paste the
complete registered callback URL from the browser, including `code`, `state` and issued `client_id`.
A bare code cannot verify flow ownership and is rejected even though the unchanged upstream panel
mentions that alternative. Pending flows expire after ten minutes and are consumed before exchange.
The default registered URI is `http://127.0.0.1:1455/auth/callback`; the web host does not create a
loopback listener. A remote browser's loopback redirect does not reach the server.

OpenAI's [remote deployment guidance](https://developers.openai.com/siwc/token-sharing-open-source/self-hosted-vms)
requires a supported local sign-in or secure credential transfer for a remote VM. Configure a registered
host callback only when OpenAI supports that exact deployment. `automatic_callback=false` is explicit;
no hosted automatic OAuth mode is claimed. Controlled signed-issuer tests and real C# manual-callback
browser tests pass; live account authorization, deployment eligibility and plan inference remain an
operator release gate. Tests never call paid inference or a live issuer.

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
