using System.Text.Json.Nodes;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Avatars and themes served at the site root (port of the app extension's /avatar/user,
/// /agents/avatar, /user/avatar, /agents/avatar and /themes routes).
/// Bundled themes are synced from llms-py into chat/themes/**; per-user overrides live under
/// App_Data/chat/user/&lt;user&gt;/themes/.
/// </summary>
public partial class AppExtension
{
    void RegisterAvatarRoutes(ExtensionContext ctx)
    {
        // leading '/' escapes the /ext/app prefix
        Ctx.AddGet("/avatar/user", req => Task.FromResult(GetAvatar(req, isAgent: false))!);
        Ctx.AddGet("/agents/avatar", req => Task.FromResult(GetAvatar(req, isAgent: true))!);
        Ctx.AddPost("/user/avatar", req => UploadAvatarAsync(req, "avatar"));
        Ctx.AddPost("/agents/avatar", req => UploadAvatarAsync(req, "agent"));

        Ctx.AddGet("/themes", req => Task.FromResult<object?>(GetThemes(req)));
        Ctx.AddGet("/themes/{theme}/ui/{file_name}", req =>
        {
            var theme = req.GetPathParam("theme");
            var fileName = req.GetPathParam("file_name");
            if (fileName.Contains("..") || theme.Contains(".."))
                return Task.FromResult<object?>(ChatResult.NotFound());

            // user overrides first, then the bundled themes synced from llms-py
            foreach (var themesDir in ThemeRoots(req).AsEnumerable().Reverse())
            {
                var path = Path.Combine(themesDir, theme, "ui", fileName);
                if (File.Exists(path) && File.Exists(Path.Combine(themesDir, theme, "theme.json")))
                    return Task.FromResult<object?>(new ChatFileResult(path));
            }
            var bundled = HostContext.VirtualFileSources.GetFile($"chat/themes/{theme}/ui/{fileName}");
            return Task.FromResult<object?>(bundled != null
                ? new ChatResult { Body = bundled.ReadAllBytes(), ContentType = MimeTypes.GetMimeType(fileName) }
                : ChatResult.NotFound());
        });
    }

    // ── Themes ──

    /// <summary>Filesystem theme dirs, lowest precedence first (bundled themes come from the VFS)</summary>
    List<string> ThemeRoots(ChatRequestContext req)
    {
        var roots = new List<string> { Path.Combine(Ctx.GetUserPath(), "themes") };
        var user = req.UserName;
        if (user != null)
            roots.Add(Path.Combine(Ctx.GetUserPath(user), "themes"));
        return roots;
    }

    /// <summary>
    /// Themes reference their assets with site-root urls, e.g. url(/themes/nord/ui/bg.webp), which
    /// only resolve when the UI is mounted at the root as llms-py does. Rebase them onto RoutePrefix
    /// as they're served, so the synced theme.json files stay identical to the Python originals
    /// (the same approach TransformUiFile takes for ai.mjs).
    /// </summary>
    JsonObject RebaseThemeUrls(JsonObject config)
    {
        var prefix = Ctx.Feature.RoutePrefix;
        if (prefix.Length == 0 || config.GetObject("vars") is not { } vars)
            return config;

        foreach (var entry in vars.ToList())
        {
            if (entry.Value is JsonValue v && v.TryGetValue<string>(out var value)
                && value.Contains("url(/themes/"))
            {
                vars[entry.Key] = value.Replace("url(/themes/", $"url({prefix}/themes/");
            }
        }
        return config;
    }

    JsonObject GetThemes(ChatRequestContext req)
    {
        var themes = new JsonObject();

        // bundled themes (chat/themes/<name>/theme.json)
        foreach (var dir in HostContext.VirtualFileSources.GetDirectory("chat/themes")?.Directories ?? [])
        {
            var themeJson = HostContext.VirtualFileSources.GetFile($"chat/themes/{dir.Name}/theme.json");
            if (themeJson != null && ChatJson.TryParseObject(themeJson.ReadAllText()) is { } config)
                themes[dir.Name] = RebaseThemeUrls(config);
        }

        // user overrides
        foreach (var themesDir in ThemeRoots(req))
        {
            if (!Directory.Exists(themesDir))
                continue;
            foreach (var themeDir in Directory.GetDirectories(themesDir))
            {
                var configPath = Path.Combine(themeDir, "theme.json");
                if (!File.Exists(configPath))
                    continue;
                if (ChatJson.TryParseObject(File.ReadAllText(configPath)) is { } config)
                    themes[Path.GetFileName(themeDir)] = RebaseThemeUrls(config);
            }
        }
        return themes;
    }

    JsonObject? GetThemeConfig(string theme, ChatRequestContext req)
    {
        foreach (var themesDir in ThemeRoots(req).AsEnumerable().Reverse())
        {
            var configPath = Path.Combine(themesDir, theme, "theme.json");
            if (File.Exists(configPath) && ChatJson.TryParseObject(File.ReadAllText(configPath)) is { } config)
                return config;
        }
        var bundled = HostContext.VirtualFileSources.GetFile($"chat/themes/{theme}/theme.json");
        return bundled != null ? ChatJson.TryParseObject(bundled.ReadAllText()) : null;
    }

    // ── Avatars ──

    object GetAvatar(ChatRequestContext req, bool isAgent)
    {
        var theme = req.QueryString("theme");
        var mode = theme == "dark" ? "dark" : "light";
        var bgColor = isAgent
            ? (mode == "dark" ? "#1e293b" : "#f3f4f6")
            : (mode == "dark" ? "#1e3a8a" : "#dbeafe");
        var textColor = mode == "dark" ? "#f1f5f9" : "#111827";

        if (theme != null && GetThemeConfig(theme, req)?.GetObject("vars") is { } vars)
        {
            mode = vars.GetString("colorScheme") ?? mode;
            bgColor = vars.GetString(isAgent ? "--assistant-bg" : "--user-bg") ?? bgColor;
            textColor = vars.GetString(isAgent ? "--assistant-text" : "--user-text") ?? textColor;
        }

        var prefix = isAgent ? "agent" : "avatar";
        var user = isAgent ? null : req.UserName;
        string[] filenames =
        [
            $"{prefix}.{mode}.webp", $"{prefix}.{mode}.png", $"{prefix}.{mode}.jpg", $"{prefix}.{mode}.jpeg",
            $"{prefix}.{mode}.svg",
            $"{prefix}.webp", $"{prefix}.png", $"{prefix}.jpg", $"{prefix}.jpeg", $"{prefix}.svg",
        ];
        foreach (var candidate in AvatarCandidates(user, filenames))
        {
            if (File.Exists(candidate))
                return new ChatFileResult(candidate) { ContentType = MimeTypes.GetMimeType(candidate) };
        }

        return new ChatResult
        {
            Text = isAgent ? DefaultAgentAvatar(bgColor, textColor) : DefaultUserAvatar(bgColor, textColor),
            ContentType = "image/svg+xml",
        };
    }

    IEnumerable<string> AvatarCandidates(string? user, string[] filenames)
    {
        if (user != null)
        {
            foreach (var filename in filenames)
                yield return Path.Combine(Ctx.GetUserPath(user), filename);
        }
        foreach (var filename in filenames)
            yield return Path.Combine(Ctx.GetUserPath(), filename);
    }

    async Task<object?> UploadAvatarAsync(ChatRequestContext req, string prefix)
    {
        var user = req.UserName;
        var userPath = Ctx.GetUserPath(prefix == "agent" ? null : user);
        Directory.CreateDirectory(userPath);

        var file = req.Request.Files.FirstOrDefault(x => x.Name == "file")
            ?? req.Request.Files.FirstOrDefault();
        if (file == null)
            throw new Exception("No file provided");

        var ext = (file.FileName ?? "").LastRightPart('.').ToLower();
        if (ext is not ("png" or "svg" or "webp" or "jpg" or "jpeg"))
            throw new Exception($"Unsupported avatar format: {ext}");

        var savePath = Path.Combine(userPath, $"{prefix}.{ext}");
        await using (var fs = File.Create(savePath))
        {
            await file.InputStream.CopyToAsync(fs).ConfigAwait();
        }
        return new JsonObject { ["success"] = true };
    }

    static string DefaultUserAvatar(string bgColor, string textColor) =>
        $"""
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" style="color:{textColor}">
            <circle cx="16" cy="16" r="16" fill="{bgColor}"/>
            <g transform="translate(4, 4)" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>
            </g>
        </svg>
        """;

    static string DefaultAgentAvatar(string bgColor, string textColor) =>
        $"""
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" style="color:{textColor}">
            <circle cx="16" cy="16" r="16" fill="{bgColor}"/>
            <path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 20v-8a2.667 2.667 0 1 1 5.333 0v8m-5.333-4h5.333m5.334-6.667v10.667" transform="translate(2.667, 1.5)"/>
        </svg>
        """;
}
