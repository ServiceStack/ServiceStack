using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

public sealed class OpenAiSubscriptionFlow(OpenAiSubscriptionOptions options, OpenAiSubscriptionStore store)
{
    sealed record Pending(string State, string Nonce, string Verifier, string ClientId, string Redirect, DateTimeOffset Created, long Generation, string? Previous, string? Subject, bool Automatic, string? ReturnUrl);
    readonly Dictionary<string, Pending> pending = new(StringComparer.Ordinal);
    readonly Dictionary<string, (string Message, DateTimeOffset Created)> errors = new(StringComparer.Ordinal);
    readonly object flowsGate = new();
    readonly CancellationTokenSource shutdown = new();
    readonly OpenAiSubscriptionHttp http = new(options);
    public OpenAiSubscriptionIdentity Identity { get; } = new(options, new OpenAiSubscriptionHttp(options));
    public bool HasPending(string user)
    { lock (flowsGate) { Expire(); return pending.ContainsKey(user); } }
    public string? CallbackError(string user)
    { lock (flowsGate) { Expire(); return errors.GetValueOrDefault(user).Message; } }
    void Expire()
    {
        foreach (var entry in pending.Where(x => options.Clock.GetUtcNow() - x.Value.Created >= options.FlowLifetime).ToArray()) pending.Remove(entry.Key);
        foreach (var entry in errors.Where(x => options.Clock.GetUtcNow() - x.Value.Created >= options.FlowLifetime).ToArray()) errors.Remove(entry.Key);
    }
    static string Random() => OpenAiSubscriptionIdentity.Base64Url(RandomNumberGenerator.GetBytes(48));
    public JsonObject Connect(string user, string? redirectUri = null, bool automatic = false, string? returnUrl = null)
    {
        shutdown.Token.ThrowIfCancellationRequested();
        redirectUri ??= options.RedirectUri;
        OpenAiSubscriptionHttp.Endpoint(options.AuthorizationUrl);
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var redirect) || redirect.UserInfo.Length != 0 || redirect.Query.Length != 0 || redirect.Fragment.Length != 0
            || redirect.Scheme != "https" && !(redirect.Scheme == "http" && redirect.Host == "127.0.0.1" && redirect.AbsolutePath == "/auth/callback"))
            throw HttpError.BadRequest("Configure the registered callback URI.");
        var state = store.State(user); Pending flow;
        lock (state.Gate)
        {
            var credentials = store.Load(user); var client = credentials.GetString("client_id") ?? options.ClientId;
            state.Generation++;
            flow = new(Random(), Random(), Random(), client, redirectUri, options.Clock.GetUtcNow(), state.Generation, OpenAiSubscriptionStore.Fingerprint(credentials), credentials.GetString("subject"), automatic, returnUrl);
        }
        lock (flowsGate)
        {
            Expire(); if (pending.Count >= 1024 && !pending.ContainsKey(user)) throw new HttpError(429, "TooManyFlows", "Too many pending sign-ins. Try again later."); pending[user] = flow; errors.Remove(user);
        }
        var query = new Dictionary<string, string> {
            ["response_type"] = "code", ["client_id"] = flow.ClientId, ["redirect_uri"] = flow.Redirect,
            ["scope"] = options.Scope, ["resource"] = options.Resource, ["state"] = flow.State, ["nonce"] = flow.Nonce,
            ["code_challenge_method"] = "S256", ["code_challenge"] = OpenAiSubscriptionIdentity.Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(flow.Verifier))),
            ["ext_agent_host_id"] = store.HostId(),
        };
        if (flow.ClientId == "dynamic_agent_client") query["agent_name_hint"] = options.AgentName;
        // Avoid returning retained ID-token hints to the browser; the account selector still supports reauthorization.
        var authorization = options.AuthorizationUrl + (options.AuthorizationUrl.Contains('?') ? "&" : "?") + string.Join("&", query.Select(x => Uri.EscapeDataString(x.Key) + "=" + Uri.EscapeDataString(x.Value)));
        return new() { ["auth_url"] = authorization, ["redirect_uri"] = flow.Redirect, ["port"] = redirect.Port, ["manual_callback"] = !automatic, ["automatic_callback"] = automatic };
    }
    /// <summary>The random state selects its initiating identity; loopback requests do not carry host cookies.
    /// The normal callback still verifies redirect, state, registration, PKCE and signed identity before saving.</summary>
    public async Task<(string User, string? ReturnUrl)> AutomaticCallbackAsync(string callback, CancellationToken token, string? requiredUser = null)
    {
        if (callback.Length > 16384 || !Uri.TryCreate(callback, UriKind.Absolute, out var uri)) throw HttpError.BadRequest("Invalid OpenAI callback URL.");
        var query = Query(uri); string user; Pending flow;
        lock (flowsGate)
        {
            Expire();
            var entry = pending.FirstOrDefault(x => x.Value.Automatic && x.Value.State == query.GetValueOrDefault("state"));
            if (entry.Key == null || requiredUser != null && entry.Key != requiredUser)
                throw HttpError.BadRequest("No matching active OpenAI sign-in. Start a new sign-in.");
            user = entry.Key; flow = entry.Value;
        }
        try { await CallbackAsync(user, callback, token).ConfigureAwait(false); }
        catch (HttpError e)
        {
            // Errors are server-generated and never contain the callback or credential response.
            lock (flowsGate)
                if (store.State(user).Generation == flow.Generation) errors[user] = (e.Message, options.Clock.GetUtcNow());
            throw;
        }
        return (user, flow.ReturnUrl);
    }
    static Dictionary<string, string> Query(Uri uri)
    {
        var result = new Dictionary<string, string>();
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2); var key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
            if (!result.TryAdd(key, parts.Length > 1 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : "")) throw HttpError.BadRequest("The callback has duplicate parameters.");
        }
        return result;
    }
    public async Task CallbackAsync(string user, string callback, CancellationToken token)
    {
        if (callback.Length > 16384 || !Uri.TryCreate(callback, UriKind.Absolute, out var uri)) throw HttpError.BadRequest("Paste the complete callback URL, including state. A bare authorization code cannot verify this sign-in.");
        Pending flow; var query = Query(uri);
        lock (flowsGate)
        {
            Expire(); if (!pending.TryGetValue(user, out flow!)) throw HttpError.BadRequest("No active login for this user. Start a new sign-in.");
            var expected = new Uri(flow.Redirect);
            if (uri.Scheme != expected.Scheme || uri.Host != expected.Host || uri.Port != expected.Port || uri.AbsolutePath != expected.AbsolutePath || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0
                || !query.TryGetValue("state", out var state) || state != flow.State) throw HttpError.BadRequest("Callback state, identity or redirect does not match this sign-in.");
            if (query.ContainsKey("error")) { pending.Remove(user); throw HttpError.BadRequest("OpenAI sign-in was not authorized. Start a new sign-in."); }
            if (!query.TryGetValue("code", out var code) || code.Length == 0) throw HttpError.BadRequest("The callback URL is missing an authorization code.");
            pending.Remove(user); // one exchange per code; never retry an uncertain authorization-code exchange
        }
        var clientId = query.GetValueOrDefault("client_id") ?? flow.ClientId;
        if (clientId.Length > 512 || clientId.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)) || clientId == "dynamic_agent_client"
            || flow.ClientId != "dynamic_agent_client" && clientId != flow.ClientId) throw HttpError.BadRequest("Callback client registration does not match this sign-in.");
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(token, shutdown.Token);
        var response = await http.TokenAsync(new() { ["grant_type"] = "authorization_code", ["client_id"] = clientId, ["code"] = query["code"], ["code_verifier"] = flow.Verifier, ["redirect_uri"] = flow.Redirect, ["resource"] = options.Resource }, cancel.Token).ConfigureAwait(false);
        var claims = await Identity.VerifyAsync(response.GetString("id_token") ?? "", clientId, flow.Nonce, cancel.Token).ConfigureAwait(false);
        if (flow.Subject != null && claims.GetString("sub") != flow.Subject) throw HttpError.BadRequest("The returning ChatGPT account does not match the selected registration.");
        var credentials = Credentials(response, clientId, claims);
        cancel.Token.ThrowIfCancellationRequested();
        if (!store.SaveIfCurrent(user, credentials, flow.Generation, flow.Previous)) throw HttpError.Conflict("The subscription changed while signing in. Start a new sign-in.");
    }
    public JsonObject Credentials(JsonObject response, string clientId, JsonObject claims, JsonObject? previous = null)
    {
        var access = response.GetString("access_token"); var expiry = response.GetLong("expires_in") ?? 3600;
        var scopes = response.GetString("scope") ?? previous.GetString("scope") ?? "";
        if (string.IsNullOrEmpty(access) || access.Length > 128 * 1024 || access.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)) || expiry is <= 0 or > 86400)
            throw HttpError.BadRequest("OpenAI returned invalid subscription credentials.");
        if (!scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("chatgpt.tokens.use.direct")) throw HttpError.BadRequest("Authorize ChatGPT plan usage before connecting this subscription.");
        var account = OpenAiSubscriptionIdentity.Account(claims);
        return new() { ["client_id"] = clientId, ["access_token"] = access, ["refresh_token"] = response.GetString("refresh_token") ?? previous.GetString("refresh_token"),
            ["id_token"] = response.GetString("id_token") ?? previous.GetString("id_token"), ["expires_at"] = options.Clock.GetUtcNow().ToUnixTimeSeconds() + expiry,
            ["earliest_refresh_at"] = response["earliest_refresh_at"]?.DeepClone(), ["scope"] = scopes, ["issuer"] = options.Issuer, ["subject"] = claims.GetString("sub"),
            ["account_id"] = account.GetString("account_id"), ["account"] = account, ["ext_agent_host_id"] = store.HostId() };
    }
    public async Task<JsonObject?> ValidCredentialsAsync(string user, CancellationToken token, string? rejectedToken = null)
    {
        var credentials = store.Load(user); if (credentials == null) return null;
        if (string.IsNullOrEmpty(credentials.GetString("access_token")) || string.IsNullOrEmpty(credentials.GetString("subject")) || string.IsNullOrEmpty(credentials.GetString("client_id")))
            throw HttpError.Unauthorized("Subscription credentials are invalid. Sign in again.");
        var expires = credentials.GetLong("expires_at") ?? 0; var now = options.Clock.GetUtcNow().ToUnixTimeSeconds();
        if (rejectedToken == null && now < expires - 300 || rejectedToken != null && credentials.GetString("access_token") != rejectedToken) return credentials;
        var state = store.State(user); await state.Refresh.WaitAsync(token).ConfigureAwait(false);
        try
        {
            long generation; string? fingerprint;
            lock (state.Gate)
            {
                credentials = store.Load(user); if (credentials == null) throw HttpError.Unauthorized("The ChatGPT subscription was disconnected.");
                expires = credentials.GetLong("expires_at") ?? 0; now = options.Clock.GetUtcNow().ToUnixTimeSeconds();
                if (rejectedToken == null && now < expires - 300 || rejectedToken != null && credentials.GetString("access_token") != rejectedToken) return credentials;
                generation = state.Generation; fingerprint = OpenAiSubscriptionStore.Fingerprint(credentials);
            }
            var refresh = credentials.GetString("refresh_token");
            if (string.IsNullOrEmpty(refresh) || rejectedToken == null && credentials.GetLong("earliest_refresh_at") is { } earliest && now < earliest)
            {
                if (rejectedToken == null && now < expires) return credentials;
                throw HttpError.Unauthorized("The ChatGPT subscription expired. Sign in again.");
            }
            JsonObject updated;
            try
            {
                using var cancel = CancellationTokenSource.CreateLinkedTokenSource(token, shutdown.Token);
                var clientId = credentials.GetString("client_id") ?? throw HttpError.Unauthorized("Subscription registration is missing. Sign in again.");
                var response = await http.TokenAsync(new() { ["grant_type"] = "refresh_token", ["client_id"] = clientId, ["refresh_token"] = refresh, ["resource"] = options.Resource }, cancel.Token).ConfigureAwait(false);
                var claims = response.GetString("id_token") is { } jwt ? await Identity.VerifyAsync(jwt, clientId, null, cancel.Token).ConfigureAwait(false) : new JsonObject { ["sub"] = credentials.GetString("subject") };
                if (claims.GetString("sub") != credentials.GetString("subject")) throw HttpError.Unauthorized("Subscription account changed. Sign in again.");
                updated = Credentials(response, clientId, claims, credentials);
                if (response.GetString("id_token") == null) { updated["account"] = credentials["account"]?.DeepClone(); updated["account_id"] = credentials["account_id"]?.DeepClone(); }
                cancel.Token.ThrowIfCancellationRequested();
            }
            catch (HttpError) when (rejectedToken == null && now < expires)
            {
                lock (state.Gate) if (state.Generation == generation && OpenAiSubscriptionStore.Fingerprint(store.Load(user)) == fingerprint) return credentials;
                throw HttpError.Unauthorized("The ChatGPT subscription changed during refresh.");
            }
            if (!store.SaveIfCurrent(user, updated, generation, fingerprint)) throw HttpError.Unauthorized("The ChatGPT subscription changed during refresh.");
            return updated;
        }
        finally { state.Refresh.Release(); }
    }
    public void Disconnect(string user)
    { store.Disconnect(user); lock (flowsGate) { pending.Remove(user); errors.Remove(user); } }
    public void Close() { shutdown.Cancel(); lock (flowsGate) { pending.Clear(); errors.Clear(); } }
}
