using ServiceStack.Text;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using ServiceStack.Web;

namespace ServiceStack.AI;

public partial class ChatFeature
{
    /// <summary>
    /// Optional image resize hook used for upload downscaling and ?variant= thumbnails.
    /// (width, height) are the target bounds; returns webp bytes. When null, originals are served.
    /// </summary>
    public Func<byte[], int?, int?, byte[]>? ImageTransformer { get; set; }

    /// <summary>Core routes in llms-py registration order (main.py:4843-5265)</summary>
    void RegisterCoreRoutes()
    {
        // POST /v1/chat/completions is the typed ChatServices service (unprefixed, like ai.mjs expects)
        Routes.AddGet("/models", _ => Task.FromResult<object?>(GetActiveModels()));
        Routes.AddGet("/providers", _ => Task.FromResult<object?>(ApiProviders()));
        Routes.AddGet("/status", _ =>
        {
            var (enabled, disabled) = ProviderStatus();
            return Task.FromResult<object?>(new JsonObject
            {
                ["all"] = new JsonArray(Config.GetObject("providers")?.Select(x => (JsonNode)x.Key).ToArray() ?? []),
                ["enabled"] = ToJsonArray(enabled),
                ["disabled"] = ToJsonArray(disabled),
            });
        });
        Routes.AddPost("/providers/{provider}", UpdateProviderHandlerAsync);
        Routes.AddPost("/upload", UploadHandlerAsync);
        Routes.AddGet("/ext", _ =>
        {
            var arr = new JsonArray();
            foreach (var ext in UiExtensions)
            {
                arr.Add(new JsonObject
                {
                    ["id"] = ext.GetString("id"),
                    ["path"] = RoutePrefix + ext.GetString("path"),
                });
            }
            return Task.FromResult<object?>(arr);
        });
        Routes.AddGet("/~cache/{tail:.*}", CacheHandlerAsync);
        Routes.AddGet("/ui/{path:.*}", ctx => ServeEmbeddedFileAsync(ctx, "chat/ui", ctx.GetPathParam("path")));
        Routes.AddGet("/config", ConfigHandlerAsync);
        Routes.AddGet("/prefs", ctx => Task.FromResult<object?>(AppData.GetUserPrefs(ctx.UserName)));
        Routes.AddGet("/favicon.ico", _ => Task.FromResult<object?>(ChatResult.NotFound()));

        // Identity Auth endpoints (Python: provided by credentials/github_auth extensions)
        Routes.AddGet("/auth", async ctx =>
        {
            var info = await ChatAuth.GetAuthInfoAsync(ctx.Request).ConfigAwait();
            return info == null
                ? ChatResult.Unauthorized(ErrorAuthRequired())
                : info;
        });
        Routes.AddPost("/auth/logout", async ctx =>
        {
            await ChatAuth.SignOutAsync(ctx.Request).ConfigAwait();
            return new JsonObject { ["success"] = true };
        });
    }

    static JsonArray ToJsonArray(IEnumerable<string> items) => new(items.Select(x => (JsonNode)x).ToArray());

    JsonArray ApiProviders()
    {
        var arr = new JsonArray();
        foreach (var entry in Providers)
        {
            var models = new JsonObject();
            foreach (var model in entry.Value.Models)
            {
                models[model.Key] = model.Value.Clone();
            }
            arr.Add(new JsonObject
            {
                ["id"] = entry.Key,
                ["name"] = entry.Value.Name,
                ["models"] = models,
                ["server_tools"] = ToJsonArray(entry.Value.ServerTools),
            });
        }
        return arr;
    }

    Task<object?> ConfigHandlerAsync(ChatRequestContext ctx)
    {
        var (enabled, disabled) = ProviderStatus();
        var providers = new JsonArray();
        if (Config.GetObject("providers") is { } configProviders)
        {
            foreach (var entry in configProviders)
            {
                if (entry.Value is not JsonObject provider)
                    continue;
                var dto = new JsonObject { ["id"] = entry.Key };
                if (provider.GetObject("modalities") is { } modalities)
                    dto["modalities"] = modalities.Clone();
                if (provider.GetArray("server_tools") is { Count: > 0 } serverTools)
                    dto["server_tools"] = serverTools.Clone();
                providers.Add(dto);
            }
        }
        var ret = new JsonObject
        {
            ["defaults"] = Config.GetObject("defaults")?.Clone() ?? new JsonObject(),
            ["status"] = new JsonObject
            {
                ["enabled"] = ToJsonArray(enabled),
                ["disabled"] = ToJsonArray(disabled),
            },
            ["extensions"] = ToJsonArray(InstalledExtensionNames),
            ["requiresAuth"] = ChatAuth.IsEnabled,
            ["authType"] = AuthType switch
            {
                ChatAuthType.ApiKey => "apikey",
                ChatAuthType.Credentials => "credentials",
                _ => "oauth",
            },
            ["signInUrl"] = SignInUrl,
            ["providers"] = providers,
        };
        return Task.FromResult<object?>(ret);
    }

    async Task<object?> UpdateProviderHandlerAsync(ChatRequestContext ctx)
    {
        if (ChatAuth.IsEnabled && !ChatAuth.IsAdmin(ctx.Request))
            return ChatResult.Unauthorized(ErrorAuthRequired());

        var provider = ctx.GetPathParam("provider");
        var data = await ctx.GetJsonBodyAsync().ConfigAwait();
        string? msg = null;
        if (data.GetBool("enable"))
        {
            msg = await EnableProviderAsync(provider).ConfigAwait();
            Log.LogInformation("Enabled provider {Provider} {Msg}", provider, msg ?? "");
        }
        else if (data.GetBool("disable"))
        {
            await DisableProviderAsync(provider).ConfigAwait();
            Log.LogInformation("Disabled provider {Provider}", provider);
        }
        var (enabled, disabled) = ProviderStatus();
        return new JsonObject
        {
            ["enabled"] = ToJsonArray(enabled),
            ["disabled"] = ToJsonArray(disabled),
            ["feedback"] = msg ?? "",
        };
    }

    // ── Upload + content-addressed cache (port of upload_handler/cache_handler) ──

    async Task<object?> UploadHandlerAsync(ChatRequestContext ctx)
    {
        var (isAuthenticated, _) = ChatAuth.CheckAuth(ctx.Request);
        if (!isAuthenticated)
            return ChatResult.Unauthorized(ErrorAuthRequired());

        var user = ctx.UserName;
        var file = ctx.Request.Files.FirstOrDefault(x => x.Name == "file")
            ?? ctx.Request.Files.FirstOrDefault();
        if (file == null)
            return ChatResult.Json(ChatJson.CreateErrorResponse("No file provided"), 400);

        var filename = file.FileName ?? "file";
        using var ms = new MemoryStream();
        await file.InputStream.CopyToAsync(ms).ConfigAwait();
        var content = ms.ToArray();
        var mimetype = MimeTypes.GetMimeType(filename);

        return SaveToCache(content, filename, mimetype, user);
    }

    /// <summary>Write bytes to the SHA256 content-addressed cache + .info.json sidecar, firing cache_saved filters</summary>
    public JsonObject SaveToCache(byte[] content, string filename, string mimetype, string? user,
        JsonObject? extraInfo = null)
    {
        var sha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var ext = filename.IndexOf('.') >= 0 ? filename.LastRightPart('.') : "";
        if (string.IsNullOrEmpty(ext))
            ext = MimeTypes.GetExtension(mimetype).TrimStart('.');
        if (string.IsNullOrEmpty(ext))
            ext = "bin";

        var relativePath = $"{sha256[..2]}/{sha256}.{ext}";
        var fullPath = AppData.GetCachePath(relativePath);
        var infoPath = Path.ChangeExtension(fullPath, null) + ".info.json";

        if (File.Exists(fullPath) && File.Exists(infoPath))
        {
            return ChatJson.ParseObject(File.ReadAllText(infoPath));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, content);

        var url = $"/~cache/{relativePath}";
        var info = new JsonObject
        {
            ["date"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["url"] = url,
            ["size"] = content.Length,
            ["type"] = mimetype,
            ["name"] = filename,
        };
        if (user != null)
            info["user"] = user;
        if (extraInfo != null)
        {
            foreach (var entry in extraInfo)
                info[entry.Key] = entry.Value?.DeepClone();
        }

        File.WriteAllText(infoPath, info.ToJsonString(ChatJson.Options));

        Filters.OnCacheSaved(new CacheSavedContext { Url = url, Info = info, User = user });
        return info;
    }

    Task<object?> CacheHandlerAsync(ChatRequestContext ctx)
    {
        var path = ctx.GetPathParam("tail");
        string fullPath;
        try
        {
            fullPath = AppData.GetCachePath(path);
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult<object?>(ChatResult.Forbidden());
        }
        var infoPath = Path.ChangeExtension(fullPath, null) + ".info.json";

        if (ctx.HasQueryParam("info"))
        {
            if (!File.Exists(infoPath))
                return Task.FromResult<object?>(ChatResult.NotFound());
            return Task.FromResult<object?>(new ChatResult
            {
                Text = File.ReadAllText(infoPath),
                ContentType = MimeTypes.Json,
            });
        }

        if (!File.Exists(fullPath))
            return Task.FromResult<object?>(ChatResult.NotFound());

        var mimetype = MimeTypes.GetMimeType(fullPath);
        if (ctx.HasQueryParam("download"))
        {
            var info = ChatJson.TryParseObject(File.Exists(infoPath) ? File.ReadAllText(infoPath) : null);
            var downloadName = info.GetString("name") ?? Path.GetFileName(fullPath);
            return Task.FromResult<object?>(new ChatFileResult(fullPath)
            {
                ContentType = info.GetString("type") ?? mimetype,
                Headers = new()
                {
                    ["Content-Disposition"] = $"attachment; filename=\"{downloadName}\"",
                },
            });
        }

        if (ctx.HasQueryParam("variant") && mimetype.StartsWith("image/") && !mimetype.Contains("svg"))
        {
            var variant = ctx.QueryString("variant") ?? "";
            int? w = null, h = null;
            foreach (var part in variant.Split(','))
            {
                if (part.StartsWith("width=") && int.TryParse(part.RightPart('='), out var pw)) w = pw;
                else if (part.StartsWith("height=") && int.TryParse(part.RightPart('='), out var ph)) h = ph;
            }
            if ((w != null || h != null) && ImageTransformer != null)
            {
                var basePath = Path.ChangeExtension(fullPath, null);
                var previewPath = w != null && h != null
                    ? $"{basePath}_{w}w_{h}h.webp"
                    : w != null ? $"{basePath}_{w}w.webp" : $"{basePath}_{h}h.webp";
                if (File.Exists(previewPath))
                    return Task.FromResult<object?>(new ChatFileResult(previewPath) { ContentType = "image/webp" });
                try
                {
                    var webp = ImageTransformer(File.ReadAllBytes(fullPath), w, h);
                    File.WriteAllBytes(previewPath, webp);
                    return Task.FromResult<object?>(new ChatFileResult(previewPath) { ContentType = "image/webp" });
                }
                catch (Exception e)
                {
                    Log.LogError(e, "Failed to generate image preview for {Path}", fullPath);
                }
            }
        }

        return Task.FromResult<object?>(new ChatFileResult(fullPath) { ContentType = mimetype });
    }

    // ── Static UI files + SPA index (port of ui_static/index_handler) ──

    public Task<object?> ServeEmbeddedFileAsync(ChatRequestContext ctx, string baseDir, string path)
    {
        if (string.IsNullOrEmpty(path) || path.Contains(".."))
            return Task.FromResult<object?>(ChatResult.NotFound());

        var file = HostContext.VirtualFileSources.GetFile($"{baseDir}/{path}");
        if (file == null)
            return Task.FromResult<object?>(ChatResult.NotFound());

        if (RoutePrefix.Length > 0 && baseDir == "chat/ui" && TransformUiFile(path, file.ReadAllText()) is { } transformed)
        {
            return Task.FromResult<object?>(new ChatResult
            {
                Text = transformed,
                ContentType = MimeTypes.GetMimeType(path),
            });
        }

        return Task.FromResult<object?>(new ChatResult
        {
            Body = file.ReadAllBytes(),
            ContentType = MimeTypes.GetMimeType(file.Name),
        });
    }

    /// <summary>
    /// llms-py serves its UI from the site root, so the synced UI defaults to `base = ''` — we
    /// mount it under RoutePrefix instead. These are the only differences we maintain against the
    /// synced files, applied as they're served so sync.sh never clobbers them (everything else the
    /// prefix affects is handled by the UI itself). Returns null when a file needs no transform.
    /// </summary>
    string? TransformUiFile(string path, string contents) => path switch
    {
        "ai.mjs" => ReplaceUiSource(path, contents, "const base = ''", $"const base = '{RoutePrefix}'"),

        // index.mjs prefixes every route with ai.base but navigates to the bare site root, which
        // matches no route when mounted under a prefix (a blank page at "/" instead of the SignIn)
        "index.mjs" => ReplaceUiSource(path,
            ReplaceUiSource(path, contents,
                "location.pathname === '/'",
                "(location.pathname === ai.base || location.pathname === ai.base + '/')"),
            "ctx.router.push({ path: '/' })",
            "ctx.router.push({ path: ai.base + '/' })"),

        _ => null,
    };

    string ReplaceUiSource(string path, string contents, string find, string replace)
    {
        if (!contents.Contains(find))
        {
            Log.LogWarning("chat/ui/{Path} no longer contains \"{Find}\", RoutePrefix '{RoutePrefix}' " +
                           "may not be applied correctly", path, find, RoutePrefix);
            return contents;
        }
        return contents.Replace(find, replace);
    }

    /// <summary>
    /// Prefix a root-relative app URL (e.g. the "/~cache/..." urls recorded with media) with
    /// RoutePrefix. URLs are persisted prefix-free so RoutePrefix stays a deployment concern, and
    /// the UI resolves them with ai.resolveUrl — but responses the UI binds straight into markup
    /// (&lt;img :src="item.url"&gt;) need the mounted path. Idempotent (ai.resolveUrl also no-ops on an
    /// already-prefixed URL), and leaves absolute, protocol-relative and data URLs alone.
    /// </summary>
    public string? ResolveClientUrl(string? url) =>
        url == null || RoutePrefix.Length == 0 || !url.StartsWith('/') || url.StartsWith("//")
        || url.StartsWith(RoutePrefix + "/", StringComparison.OrdinalIgnoreCase)
            ? url
            : RoutePrefix + url;

    public Task<object?> IndexHandlerAsync(IRequest req)
    {
        var file = HostContext.VirtualFileSources.GetFile("chat/index.html");
        if (file == null)
            return Task.FromResult<object?>(ChatResult.NotFound("chat/index.html not found"));

        var html = file.ReadAllText();

        var imports = new JsonObject();
        foreach (var entry in ImportMaps)
        {
            imports[entry.Key] = entry.Value.StartsWith('/')
                ? RoutePrefix + entry.Value
                : entry.Value;
        }
        var importMaps = new JsonObject { ["imports"] = imports };
        html = html.Replace("<script type=\"importmap\"></script>",
            "<script type=\"importmap\">\n" + importMaps.ToJsonString(ChatJson.Indented) + "\n</script>");

        if (IndexHeaders.Count > 0)
        {
            html = html.Replace("</head>", string.Join("", IndexHeaders) + "\n</head>");
        }
        if (IndexFooters.Count > 0)
        {
            html = html.Replace("</body>", string.Join("", IndexFooters) + "\n</body>");
        }

        if (RoutePrefix.Length > 0)
        {
            // root-relative asset refs in index.html + injected headers/footers
            // (href="/ui/...", import '/ui/index.mjs', extension-contributed /ext/ links)
            html = html.Replace("\"/ui/", $"\"{RoutePrefix}/ui/")
                       .Replace("'/ui/", $"'{RoutePrefix}/ui/")
                       .Replace("\"/ext/", $"\"{RoutePrefix}/ext/")
                       .Replace("'/ext/", $"'{RoutePrefix}/ext/");
        }

        return Task.FromResult<object?>(ChatResult.Html(html));
    }
}
