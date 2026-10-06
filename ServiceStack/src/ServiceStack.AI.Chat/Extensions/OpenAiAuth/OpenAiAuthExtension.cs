using System.Text;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>Provider settings for a user's ChatGPT grant, independent of the host Identity login.</summary>
public sealed class OpenAiAuthExtension() : ChatExtension("openai_auth")
{
    public OpenAiSubscriptionOptions Options { get; set; } = new();
    public OpenAiSubscriptionStore Credentials { get; private set; } = null!;
    public OpenAiSubscriptionFlow Flows { get; private set; } = null!;
    OpenAiSubscriptionCallback callback = null!;
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string Fingerprint, DateTimeOffset Time, JsonArray Models)> catalogs = new(StringComparer.Ordinal);
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> catalogGates = new(StringComparer.Ordinal);
    // IdentityChatAuth returns null in RequireAuth=false mode; the storage partition is still "default".
    static string User(ChatRequestContext request) => request.AssertUserName() ?? ChatDb.DefaultUser;
    static async Task<JsonObject> Body(ChatRequestContext request)
    {
        byte[] bytes;
        if (request.Request is Host.BasicRequest) bytes = Encoding.UTF8.GetBytes(await request.Request.GetRawBodyAsync().ConfigureAwait(false) ?? "");
        else
        {
            using var output = new MemoryStream(); var buffer = new byte[16384];
            while (true)
            {
                var count = await request.Request.InputStream.ReadAsync(buffer, request.Request.RequestAborted).ConfigureAwait(false); if (count == 0) break;
                if (output.Length + count > 32 * 1024) throw HttpError.BadRequest("Provide a subscription request under 32 KB."); output.Write(buffer, 0, count);
            }
            bytes = output.ToArray();
        }
        if (bytes.Length > 32 * 1024) throw HttpError.BadRequest("Provide a subscription request under 32 KB.");
        string raw; try { raw = new UTF8Encoding(false, true).GetString(bytes); } catch (DecoderFallbackException) { throw HttpError.BadRequest("Provide valid UTF-8 JSON."); }
        return ChatJson.TryParseObject(raw) ?? throw HttpError.BadRequest("Provide a JSON object.");
    }
    public override void Install(ExtensionContext ctx)
    {
        Credentials = new(ctx); Flows = new(Options, Credentials); ctx.RegisterShutdownHandler(Flows.Close);
        ctx.AddGet("status", request => Task.FromResult<object?>(Status(request)));
        callback = new(Flows, user => { Activate(); catalogs.TryRemove(user, out _); });
        ctx.RegisterShutdownHandler(callback.CloseAsync);
        ctx.AddPost("connect", ConnectAsync);
        ctx.AddGet("callback", async request =>
        {
            if (!Options.AutomaticCallback || new Uri(Options.RedirectUri).Scheme != "https") return ChatResult.NotFound();
            var result = await Flows.AutomaticCallbackAsync(request.Request.AbsoluteUri, request.Request.RequestAborted, User(request)).ConfigureAwait(false);
            Activate(); catalogs.TryRemove(result.User, out _);
            var page = ChatResult.Html(OpenAiSubscriptionCallback.Page(result.ReturnUrl));
            page.Headers = new() { ["Cache-Control"] = "no-store", ["Referrer-Policy"] = "no-referrer" }; return page;
        });
        ctx.AddPost("callback_manual", async request =>
        {
            var user = User(request); var body = await Body(request).ConfigureAwait(false);
            await Flows.CallbackAsync(user, body.GetString("url_or_code")?.Trim() ?? "", request.Request.RequestAborted).ConfigureAwait(false);
            Activate(); catalogs.TryRemove(user, out _); return new JsonObject { ["success"] = true };
        });
        ctx.AddPost("disconnect", request =>
        {
            var user = User(request); Flows.Disconnect(user); catalogs.TryRemove(user, out _);
            // Keep the wrapper for other users and API-key fallback. No shared headers contain a user's token.
            return Task.FromResult<object?>(new JsonObject { ["success"] = true, ["connected"] = false });
        });
        ctx.AddPost("import_codex", ImportAsync);
        ctx.AddGet("models", async request => await AccountModelsAsync(User(request), request.Request.RequestAborted).ConfigureAwait(false));
        var previous = ctx.Feature.ModelCatalogFilter;
        ctx.Feature.ModelCatalogFilter = async (request, models) =>
        {
            if (previous != null) models = await previous(request, models).ConfigureAwait(false);
            var user = User(request); var grant = Credentials.Load(user);
            if (grant == null) return models;
            var accountModels = await AccountModelsAsync(user, request.Request.RequestAborted).ConfigureAwait(false);
            var filtered = new JsonArray(models.OfType<JsonObject>().Where(m => m.GetString("provider") != "openai").Select(m => (JsonNode)m.Clone()).ToArray());
            foreach (var model in accountModels) filtered.Add(model?.DeepClone()); return filtered;
        };
    }
    async Task<object?> ConnectAsync(ChatRequestContext request)
    {
        var user = User(request); var body = await Body(request).ConfigureAwait(false);
        var returnUrl = ReturnUrl(request, body.GetString("return_url"));
        var redirect = Options.RedirectUri;
        if (Options.AutomaticCallback)
        {
            if (!Uri.TryCreate(redirect, UriKind.Absolute, out var configured)) throw HttpError.BadRequest("Configure a valid OpenAI callback URI.");
            if (configured.Scheme == "https")
            {
                var path = Feature.RoutePrefix.TrimEnd('/') + "/ext/openai_auth/callback";
                if (configured.AbsolutePath != path) throw HttpError.BadRequest("Set the registered HTTPS OpenAI callback to " + path + ".");
            }
            else redirect = await callback.StartAsync(redirect, request.Request.RequestAborted).ConfigureAwait(false);
        }
        return Flows.Connect(user, redirect, Options.AutomaticCallback, returnUrl);
    }
    string? ReturnUrl(ChatRequestContext request, string? supplied)
    {
        if (!Uri.TryCreate(request.Request.AbsoluteUri, UriKind.Absolute, out var origin))
        {
            if (!string.IsNullOrEmpty(supplied)) throw HttpError.BadRequest("Cannot verify the sign-in return URL.");
            return null;
        }
        var root = Feature.RoutePrefix.TrimEnd('/') + "/";
        if (string.IsNullOrEmpty(supplied)) return origin.GetLeftPart(UriPartial.Authority) + root;
        if (supplied.Length > 8192 || !Uri.TryCreate(supplied, UriKind.Absolute, out var target) || target.Scheme is not ("http" or "https")
            || target.UserInfo.Length != 0 || target.GetLeftPart(UriPartial.Authority) != origin.GetLeftPart(UriPartial.Authority)
            || !target.AbsolutePath.StartsWith(root, StringComparison.Ordinal) && target.AbsolutePath != root.TrimEnd('/'))
            throw HttpError.BadRequest("The sign-in return URL must belong to this Chat UI.");
        return target.AbsoluteUri;
    }
    public void Activate()
    {
        lock (Feature)
        {
            if (Feature.Providers.GetValueOrDefault("openai") is OpenAiSubscriptionProvider) return;
            var baseProvider = Feature.Providers.GetValueOrDefault("openai");
            // No API key is needed to reconstruct catalog/aliases for a subscription-only deployment.
            Feature.Providers["openai"] = new OpenAiSubscriptionProvider(Ctx, Options, Flows, Credentials, baseProvider);
        }
    }
    bool HasGrantFiles() => Directory.Exists(Path.Combine(Feature.AppData.BasePath, "user"))
        && Directory.EnumerateDirectories(Path.Combine(Feature.AppData.BasePath, "user")).Any(dir => File.Exists(Path.Combine(dir, "credentials", "openai_subscription.json")));
    public override Task LoadAsync(ExtensionContext ctx, CancellationToken token = default)
    { token.ThrowIfCancellationRequested(); if (HasGrantFiles()) Activate(); return Task.CompletedTask; }
    public override Task ReloadProvidersAsync(ExtensionContext ctx, CancellationToken token = default) => LoadAsync(ctx, token);
    JsonObject Status(ChatRequestContext request)
    {
        var user = User(request); var grant = Credentials.Load(user); var account = grant.GetObject("account");
        var provider = Feature.Providers.GetValueOrDefault("openai"); var baseProvider = provider is OpenAiSubscriptionProvider subscription ? subscription.BaseProvider : provider;
        var connected = !string.IsNullOrEmpty(grant.GetString("access_token")); var expires = grant.GetLong("expires_at") ?? 0;
        var hasApiKey = !string.IsNullOrEmpty(baseProvider?.ApiKey);
        // No implicit operator-file probing. Only an explicit host resolver plus authorization enables import.
        var canImport = Options.CanImportLocalCredentials?.Invoke(request) == true && Options.LocalCredentialsPath != null;
        return new() { ["connected"] = connected, ["email"] = account.GetString("email") ?? "", ["name"] = account.GetString("name") ?? "", ["plan"] = account.GetString("plan") ?? "",
            ["account_id"] = grant.GetString("account_id") ?? account.GetString("account_id") ?? "", ["expires_at"] = expires, ["expired"] = connected && Options.Clock.GetUtcNow().ToUnixTimeSeconds() >= expires,
            ["has_api_key"] = hasApiKey, ["api_key_active"] = hasApiKey && grant == null,
            ["api_key_disabled"] = hasApiKey && grant != null, ["has_codex_auth"] = canImport,
            ["pending"] = Flows.HasPending(user), ["manual_callback"] = !Options.AutomaticCallback, ["automatic_callback"] = Options.AutomaticCallback, ["callback_error"] = Flows.CallbackError(user) };
    }
    async Task<object?> ImportAsync(ChatRequestContext request)
    {
        var user = User(request);
        if (Options.CanImportLocalCredentials?.Invoke(request) != true || Options.LocalCredentialsPath?.Invoke(user) is not { } path)
            throw new HttpError(503, "LocalImportUnavailable", "Operator Codex credentials are unavailable on this host. Use Sign in with ChatGPT and paste the complete callback URL.");
        var file = new FileInfo(path); if (!file.Exists || file.Length > 1024 * 1024 || file.LinkTarget != null) throw HttpError.BadRequest("The host's selected credential file is unavailable or invalid.");
        var state = Credentials.State(user); long generation; string? previous;
        lock (state.Gate) { generation = state.Generation; previous = OpenAiSubscriptionStore.Fingerprint(Credentials.Load(user)); }
        var imported = ChatJson.TryParseObject(await File.ReadAllTextAsync(path, request.Request.RequestAborted).ConfigureAwait(false)) ?? throw HttpError.BadRequest("The selected credential file is invalid.");
        var tokens = imported.GetObject("tokens") ?? imported; var access = tokens.GetString("access_token") ?? "";
        var clientId = imported.GetString("client_id") ?? OpenAiSubscriptionIdentity.Claims(access).GetString("client_id") ?? "";
        if (clientId.Length == 0 || clientId == "dynamic_agent_client") throw HttpError.BadRequest("Import credentials issued for this application with their client_id. Operator CLI credentials are not assumed compatible.");
        var claims = await Flows.Identity.VerifyAsync(tokens.GetString("id_token") ?? "", clientId, null, request.Request.RequestAborted).ConfigureAwait(false);
        var response = tokens.Clone(); response["scope"] = imported.GetString("scope") ?? OpenAiSubscriptionIdentity.Claims(access).GetString("scope");
        response["expires_in"] = Math.Clamp((OpenAiSubscriptionIdentity.Claims(access).GetLong("exp") ?? 0) - Options.Clock.GetUtcNow().ToUnixTimeSeconds(), 0, 86400);
        var credentials = Flows.Credentials(response, clientId, claims);
        if (!Credentials.SaveIfCurrent(user, credentials, generation, previous)) throw HttpError.Conflict("The subscription changed during import. Try again.");
        Activate(); catalogs.TryRemove(user, out _); return new JsonObject { ["success"] = true, ["connected"] = true };
    }
    public async Task<JsonArray> AccountModelsAsync(string user, CancellationToken token)
    {
        var gate = catalogGates.GetOrAdd(user, _ => new(1, 1)); await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var grant = await Flows.ValidCredentialsAsync(user, token).ConfigureAwait(false) ?? throw HttpError.Unauthorized("Connect a ChatGPT subscription first.");
            var fingerprint = OpenAiSubscriptionStore.Fingerprint(grant)!;
            if (catalogs.TryGetValue(user, out var cached) && cached.Fingerprint == fingerprint && Options.Clock.GetUtcNow() - cached.Time < TimeSpan.FromMinutes(5)) return cached.Models.Clone();
            using var request = new HttpRequestMessage(HttpMethod.Get, OpenAiSubscriptionHttp.Endpoint(Options.ModelsUrl)); request.Headers.Authorization = new("Bearer", grant.GetString("access_token"));
            var data = await new OpenAiSubscriptionHttp(Options).JsonAsync(request, token).ConfigureAwait(false); var rows = data.GetArray("models") ?? throw new HttpError(502, "SubscriptionCatalog", "ChatGPT returned an invalid model catalog.");
            var models = new JsonArray();
            foreach (var row in rows.OfType<JsonObject>().Where(x => x.GetString("visibility") == "list"))
            {
                var slug = row.GetString("slug"); if (string.IsNullOrEmpty(slug) || slug.Length > 512) continue;
                models.Add(new JsonObject { ["id"] = slug, ["name"] = row.GetString("display_name") ?? slug, ["provider"] = "openai", ["tool_call"] = true,
                    ["cost"] = new JsonObject { ["input"] = 0d, ["output"] = 0d }, ["modalities"] = new JsonObject { ["input"] = new JsonArray("text", "image"), ["output"] = new JsonArray("text") } });
            }
            if (OpenAiSubscriptionStore.Fingerprint(Credentials.Load(user)) != fingerprint) throw HttpError.Conflict("The subscription changed while listing models. Reload the model picker.");
            Activate(); var provider = (OpenAiSubscriptionProvider)Feature.Providers["openai"]; provider.Discover(models, user);
            catalogs[user] = (fingerprint, Options.Clock.GetUtcNow(), models.Clone()); return models;
        }
        finally { gate.Release(); }
    }
}
