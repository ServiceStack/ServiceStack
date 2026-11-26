# OAuth sign-in security

How ServiceStack's OAuth providers protect the sign-in flow against account takeover and login CSRF, and why each check exists. These changes address [GHSA-99jx-gwp7-4j69](https://github.com/ServiceStack/ServiceStack/security/advisories/GHSA-99jx-gwp7-4j69) and the related issues found while fixing it.

| Threat | Protection | Code |
|---|---|---|
| A victim opens an attacker's callback URL and the attacker's identity is linked to the victim's account | Random per-provider `state` stored in the session, required and checked on every callback | `OAuthProvider.CreateOAuthState` / `ValidateOAuthState` |
| A victim opens an attacker's callback URL and is signed in to the attacker's session (login CSRF) | Sessions are never loaded from `state`. Callbacks are bounced through a same-site page instead | `OAuthProvider.Init`, `OAuthProvider.CreateSessionCookieBounce`, `AuthenticateService` |
| A link or cross-site form links an attacker's access token to the signed-in user, or signs a victim in to the attacker's account | `?AccessToken=` sign-in needs a same-origin POST | `OAuthProvider.IsAccessTokenAuthAllowed` |
| An identity already linked to one user is moved to another | Repositories refuse to re-parent a provider link | `AccountAlreadyLinkedException` |
| A new OAuth identity is merged into an existing account because its provider username or unverified email matches | Repositories resolve OAuth identities by provider link, and by email only when the provider verified it | `UserAuthRepositoryExtensions.HasProviderUserId` / `IsEmailVerified` |

The same changes are made to the sync providers (`OAuthProviderSync`, `OAuth2ProviderSync`, `AuthProviderSync`).

## 1. OAuth `state`

**Why:** previously OAuth2 used the session id as `state`, and only rejected a mismatched `state` when one was stored. A callback with no stored state, such as one an attacker forwarded from their own sign-in, was accepted and linked the attacker's identity to whoever was signed in. The session id was also sent to the identity provider, where it could end up in logs and `Referer` headers.

**How it works:**

- `CreateOAuthState(session, tokens)` generates a random value with `SessionExtensions.CreateRandomSessionId()` and stores it in `AuthUserSession.Meta`, or in `tokens.Items` for custom session types. The provider saves the session before redirecting to the identity provider.
- The value is stored under `OAuthStateSessionKey`, which defaults to `"oauthstate:" + Provider`. Each provider has its own key, so starting a GitHub sign-in in one tab doesn't break a Google sign-in started in another. Starting the same provider twice, such as with a double click, still replaces the first sign-in's state, so only the latest one can complete.
- `ValidateOAuthState(request, session, tokens)` fails when the callback has no `state`, when nothing is stored for the provider, or when the values differ. Values are compared with `CryptUtils.FixedTimeEquals`. On success the stored value is removed and the session is saved, so a `state` can only be used once.
- A failed check redirects to the referrer with `f=InvalidState`.

Providers that check `state`: every `OAuth2Provider` subclass (Google, Microsoft Graph, LinkedIn, Discord, Spotify, Apple, and custom ones), Facebook, GitHub, VK, Yandex, Yammer and Odnoklassniki. Twitter uses OAuth 1.0a, which has no `state`. Its request token secret is held in the session and serves the same purpose.

Both methods are `protected virtual`, so custom providers that don't derive from `OAuth2Provider` should call them the same way: `CreateOAuthState` when building the authorize URL, and `ValidateOAuthState` before exchanging the `code`.

## 2. `RestoreSessionFromState` and the session cookie bounce

**Why it exists:** with `HostConfig.UseSameSiteCookies = true`, session cookies are `SameSite=Strict`. Browsers don't send them on the identity provider's redirect back to the callback, because that navigation starts on another site. Without the session, the callback can't find the stored `state` or the session it should sign in. `RestoreSessionFromState` exists to solve this. It defaults to `UseSameSiteCookies == true`, and `AppleAuthProvider` always enables it because Apple's `form_post` callback is a cross-site POST.

**The old approach and why it was removed:** `Init()` put the session id in `state`, then on the callback cleared the request's cookies and loaded whichever session `state` named. Anyone holding a callback URL for their own session could therefore make a victim's browser load it, signing the victim in as the attacker. This also affected Twitter: it never sends `state` back, but an attacker could append `&state=<their session id>` to their own Twitter callback, and the restored session still held the Twitter request token secret.

**How it works now:** no session is ever loaded from a URL parameter. Instead, when `RestoreSessionFromState` is enabled, `AuthenticateService` answers an OAuth callback with a small HTML page that resends the same request from this App's own page. Browsers send `SameSite=Strict` cookies on a navigation started by a same-site page, so the second request carries the session cookie and is checked normally.

- `OAuthProvider.CreateSessionCookieBounce(req)` treats a request as a callback when it has a `state`, `code` or `oauth_token` parameter.
- **GET callbacks** (most providers) get a page that reloads the same path and query with `ss-bounced=1` appended, using `<meta http-equiv="refresh">`, `window.location.replace()`, and a Continue link for when both are blocked.
- **POST callbacks** (Apple's `form_post`) get a page with a form that auto-submits the same fields to the same path with `ss-bounced=1`, plus a visible Continue button for when scripts are blocked. Field names and values are HTML-encoded.
- Requests that already have `ss-bounced` (`OAuthProvider.SessionCookieBounceParam`) are processed normally. Requests that aren't callbacks, and in-process requests, are never bounced.
- The target is always the request's own path, built from `PathAndQuery`, so the page can't be used as an open redirect. Leading slashes and backslashes are collapsed to a single `/`, so a request for `//evil.com/...` or `/\evil.com/...` can't produce a protocol-relative URL to another site.
- The form is submitted with `HTMLFormElement.prototype.submit.call(document.forms[0])`, because a field named `submit` would otherwise replace the form's `submit()` method.
- `AuthenticateService` reads `state` from the query string or the form when deciding whether to keep the session during an OAuth flow. Otherwise Apple's re-submitted `form_post` callback would be given new session cookies before the provider validated it.
- The page is sent with `Cache-Control: no-store` and `Referrer-Policy: no-referrer` (also as a `<meta name="referrer">`), so the authorization `code` isn't cached or sent to other sites.

**Why a forged link still fails:** if an attacker sends a victim a callback URL that already contains `ss-bounced=1`, it isn't bounced. It arrives cross-site without the victim's `SameSite=Strict` session cookie, so there's no stored `state` to match, and `ValidateOAuthState` rejects it with `f=InvalidState`.

**Requirements and side effects:**

- Sign-in takes one extra request when `RestoreSessionFromState` is enabled. Apple's callback needs JavaScript, or the user clicks Continue.
- The page uses an inline script and `onload` handler. With a Content Security Policy that blocks inline scripts, GET callbacks still work through the `<meta>` refresh, and POST callbacks need the user to click Continue.
- The approach relies on browsers sending `SameSite=Strict` cookies on a navigation started by a same-site page after a cross-site redirect, as the SameSite specification describes. Verify sign-in in each target browser when changing this code.
- Non-browser clients that call a callback URL directly with `code` and `state` now receive the HTML page instead of being signed in.

**Rejected alternative:** a random nonce in a separate `SameSite=None` cookie, with `state` mapped to the session id in the cache. It also prevents login CSRF, but needs a cookie outside the App's global SameSite setting, a workaround for browsers that mishandle `SameSite=None`, and shared cache entries. The bounce needs none of these. It's the fallback if a browser is found that doesn't send the session cookie on the bounced request.

## 3. Signing in with an `AccessToken`

**Why:** `Authenticate` requests with `AccessToken` (and `AccessTokenSecret` for Twitter) let mobile and desktop apps sign in with a token they obtained themselves. The provider verifies the token, then links its identity to the current session. A GET link such as `/auth/google?AccessToken=<attacker token>` opened by a signed-in user linked the attacker's identity to that user's account. This branch runs before any `state` check, so the `state` fix didn't cover it.

**How it works:** `IsAccessTokenAuthAllowed(authService, session)` runs before the token is verified in OAuth2 (sync and async), Facebook, GitHub, Twitter and VK. It returns `false`, and the provider returns `403 Forbidden` with "AccessToken authentication requires a same-origin POST request", when:

- the request is a GET, or
- the request comes from another site, as determined by `IsTrustedOrigin`.

The origin is checked whether or not the session is signed in. For a signed-in user it stops a cross-site form linking the attacker's identity to their account. For a signed-out user it stops a cross-site form signing them in to the attacker's account (login CSRF).

`IsTrustedOrigin` reads the `Origin` header, or `Referer` when `Origin` is missing, and accepts:

- the same host as the request. Its scheme and port aren't compared, because a TLS-terminating reverse proxy can change both, except that an `http://` origin is rejected for an `https://` request;
- the same scheme, host and port as `HostConfig.WebHostUrl`;
- an origin in `CorsFeature.AllowOriginWhitelist`.

Requests with neither header, such as native apps and service clients, are trusted: browsers send `Origin` on cross-site POSTs. A value that isn't an `http` or `https` URL, including `Origin: null`, is rejected.

The origin check matters most when `UseSameSiteCookies = false`, where cookies are `SameSite=None` and a cross-site form POST still carries the session cookie. Service clients are unaffected, since `Authenticate` is sent as a POST. Override `IsAccessTokenAuthAllowed` to change the policy.

## 4. Identities already linked to another user

**Why:** a callback for an identity that's already linked to user B, completed in user A's session, re-parented the link to A.

**How it works:** `CreateOrMergeAuthSession` and `CreateOrMergeAuthSessionAsync` throw `AccountAlreadyLinkedException` when the existing `UserAuthDetails` for the provider and user id belongs to a different `UserAuthId`. The exception derives from `InvalidOperationException`, so existing `catch` blocks still handle it.

`AuthProvider.OnAuthenticatedAsync` and `AuthProviderSync.OnAuthenticated` catch it and redirect to the referrer with `f=AccountAlreadyLinked`, instead of the previous HTTP 500. The session is removed only when this sign-in authenticated it (`session.UserAuthId` is empty), the same way a failed `ValidateAccount` does. A user who was already signed in stays signed in, because their saved session hasn't been changed.

Repositories that throw it: OrmLite, Redis and InMemory, MongoDb, RavenDb and DynamoDb, sync and async.

## 5. Matching an OAuth identity to an existing account

**Why:** when the session wasn't signed in, repositories looked up `session.UserAuthName` before the provider link. Providers set `UserAuthName` from data the user controls: GitHub uses the login, OAuth2 providers use the email. Registering the GitHub login `admin` was enough to be signed in as the local `admin` account, with no click from any victim.

**How it works:** `GetUserAuth(session, tokens)` in every repository now resolves an account in this order:

1. `session.UserAuthId`, when the session is already signed in.
2. `session.UserAuthName`, but only when the tokens don't identify an OAuth identity (`tokens.HasProviderUserId()` is false). Windows auth and other non-OAuth providers still match by username.
3. The existing provider link: `UserAuthDetails` with the same `Provider` and `UserId`.
4. The account with the same email, only when `tokens.IsEmailVerified()` is true.

`IsEmailVerified()` is true when the tokens have an email containing `@` and `tokens.Items` contains `email_verified` or `verified_email` set to `true` (case-insensitive):

- **GitHub** sets `email_verified` when its primary email from `/user/emails` is verified. That endpoint is only called when the profile has no public email. GitHub's `/user` profile has no verified flag, so a public profile email is never treated as verified.
- **Google** returns `verified_email` from its user info endpoint.
- **Apple** returns `email_verified` in its identity token.
- **Discord** rejects sign-ins without a verified email, and sets `email_verified`.

Google's, Apple's and Discord's values are copied into `tokens.Items` only while `SaveExtendedUserInfo` is enabled, which is the default. Providers that don't report a verified email, including Facebook, Microsoft Graph and LinkedIn, never merge into an existing account by email. Custom providers can opt in by setting `tokens.Items["email_verified"] = "true"` after confirming the email with their provider.

**Effect on users:** with the default `ValidateUniqueUserNames` and `ValidateUniqueEmails`, a new OAuth identity whose username or unverified email belongs to an existing account is rejected with `f=UserNameAlreadyExists` or `f=EmailAlreadyExists`. To link it, the user signs in to their existing account first, then signs in with the provider.

## Upgrade notes

- Sign-ins that are in progress during the upgrade fail once with `f=InvalidState`, because their stored state uses the old key or format.
- Handle the new `f=AccountAlreadyLinked` failure value wherever `f=InvalidState`, `f=UserNameAlreadyExists` and similar are displayed.

## Tests

[`OAuth2StateSecurityTests.cs`](../../../tests/ServiceStack.WebHost.Endpoints.Tests/OAuth2StateSecurityTests.cs) covers:

- missing, mismatched and replayed `state`, and providers' sign-ins running at the same time
- `AccessToken` sign-in rejected over GET and over a cross-site POST, signed in or not, and which origins are trusted
- `AccountAlreadyLinkedException` in the OrmLite and InMemory repositories, and the `f=AccountAlreadyLinked` redirect keeping a signed-in user signed in
- OAuth identities not matched by provider username, and by email only when verified
- `RestoreSessionFromState` not using the session id as `state`, and the GET and form POST bounce pages, including protocol-relative paths and a form field named `submit`

The bounce's reliance on browser cookie behavior isn't covered by unit tests. Check it manually with `UseSameSiteCookies = true` by signing in with Google (GET callback) and Apple (POST callback) in Chrome, Firefox and Safari.
