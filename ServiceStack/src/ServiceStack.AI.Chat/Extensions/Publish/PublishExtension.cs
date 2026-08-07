using Microsoft.Extensions.Logging;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Publish threads/media/projects to a remote llms.py site (port of llms-py's "publish" extension).
/// Connection config is stored per user at App_Data/chat/user/&lt;user&gt;/publish/config.json.
/// </summary>
public partial class PublishExtension : ChatExtension
{
    const string DefaultPublishBaseUrl = "https://ai.llmspy.org";
    const string RegisterPath = "/embed/register.html?domain=llmspy.org";

    public PublishExtension() : base("publish")
    {
        Enabled = false;
    }

    public override void Install(ExtensionContext ctx)
    {
        ctx.AddGet("config.json", req =>
            Task.FromResult<object?>(GetPublishConfig(req.UserName)));

        ctx.AddPost("disconnect", req =>
        {
            var configPath = ConfigPath(req.UserName);
            if (File.Exists(configPath))
                File.Delete(configPath);
            return Task.FromResult<object?>(GetPublishConfig(req.UserName));
        });

        ctx.AddPost("config.json", async req =>
        {
            var user = req.UserName;
            var body = await req.GetJsonBodyAsync().ConfigAwait();
            var existing = GetPublishConfig(user, obscure: false);
            if (body.GetString("apiKey").IsNullOrEmpty() && existing.GetString("apiKey") is { } apiKey)
            {
                body["apiKey"] = apiKey;
            }
            foreach (var entry in body)
            {
                existing[entry.Key] = entry.Value?.DeepClone();
            }
            SaveConfig(user, existing);
            return GetPublishConfig(user);
        });

        ctx.AddGet("detect-dist", req => Task.FromResult<object?>(DetectDist(req.UserName)));
        ctx.AddGet("list-subdirs", req => Task.FromResult<object?>(ListSubdirs(req)));

        ctx.AddGet("thread/{id}", req =>
        {
            var threadId = long.Parse(req.GetPathParam("id"));
            var thread = ctx.Threads.GetThread(threadId, req.UserName)
                ?? throw new Exception($"Thread {threadId} not found");
            return Task.FromResult<object?>(thread);
        });

        ctx.AddPost("thread/{id}", PublishThreadAsync);
        ctx.AddPost("project/{name}", PublishProjectAsync);
        ctx.AddPost("media/{id}", PublishMediaAsync);
    }

    // ── Config ──

    string ConfigPath(string? user) =>
        Path.Combine(Ctx.GetUserPath(user), "publish", "config.json");

    JsonObject GetPublishConfig(string? user, bool obscure = true)
    {
        var candidatePaths = new List<string>();
        if (user != null)
            candidatePaths.Add(ConfigPath(user));
        candidatePaths.Add(ConfigPath(null));

        var obj = new JsonObject { ["apiKey"] = null, ["userName"] = null, ["userId"] = null };
        foreach (var path in candidatePaths)
        {
            if (!File.Exists(path))
                continue;
            if (ChatJson.TryParseObject(File.ReadAllText(path)) is { } config)
            {
                obj = config;
                if (obscure && obj.GetString("apiKey") is { Length: > 7 } apiKey)
                {
                    obj["apiKey"] = apiKey[..3] + "******" + apiKey[^4..];
                }
            }
        }

        obj["registerUrl"] ??= (obj.GetString("baseUrl") ?? DefaultPublishBaseUrl) + RegisterPath;
        return obj;
    }

    void SaveConfig(string? user, JsonObject config)
    {
        var path = ConfigPath(user);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, config.ToJsonString(ChatJson.Indented));
    }

    string BaseUrl(JsonObject config) => config.GetString("baseUrl") ?? DefaultPublishBaseUrl;

    string RequireApiKey(JsonObject config) => config.GetString("apiKey")
        ?? throw new Exception("No API key configured");

    // ── Project directory discovery ──

    JsonObject? ActiveProject(string? user)
    {
        var activeProject = Ctx.GetUserPref("project", user)?.GetValue<string>();
        if (activeProject == null)
            return null;
        return Ctx.Projects.GetUserProjects(user)
            .FirstOrDefault(p => p.GetString("name") == activeProject);
    }

    /// <summary>The active project's publish directory, relative to its project folder ("" = project root)</summary>
    JsonObject DetectDist(string? user)
    {
        var project = ActiveProject(user);
        if (project == null)
            return new JsonObject { ["dist"] = "" };

        var projectDir = ProjectsExtension.GetProjectDir(Ctx.GetUserPath(user), project);
        var publish = ProjectsExtension.SanitizePublishPath(project.GetString("publish"), projectDir);
        if (publish.Length > 0)
            return new JsonObject { ["dist"] = publish };

        return new JsonObject { ["dist"] = Directory.Exists(Path.Combine(projectDir, "dist")) ? "dist" : "" };
    }

    /// <summary>
    /// Folder browser for the publish dialog, confined to the project folder. Every path in and out
    /// is relative to it, so the UI never sees a server path; `displayPath` is the ~/ label to show.
    /// </summary>
    object ListSubdirs(ChatRequestContext req)
    {
        var user = req.UserName;
        var pathParam = req.QueryString("path") ?? "";
        var projectParam = req.QueryString("project");

        var activeProject = !string.IsNullOrEmpty(projectParam)
            ? projectParam
            : Ctx.GetUserPref("project", user)?.GetValue<string>();

        var userPath = Path.GetFullPath(Ctx.GetUserPath(user));
        JsonObject? project = null;
        if (!string.IsNullOrEmpty(activeProject))
        {
            project = Ctx.Projects.GetUserProjects(user).FirstOrDefault(p =>
                p.GetString("name") == activeProject || p.GetString("folder") == activeProject);
        }
        var projectDir = project != null
            ? ProjectsExtension.GetProjectDir(userPath, project)
            : userPath;

        var cleanRel = ProjectsExtension.SanitizePublishPath(pathParam, projectDir);
        var resolvedPath = Path.GetFullPath(Path.Combine(projectDir, cleanRel));

        if (!ProjectsExtension.IsWithin(resolvedPath, projectDir) || !Directory.Exists(resolvedPath))
        {
            return ChatResult.Json(new JsonObject
            {
                ["error"] = "Invalid or non-existent path",
                ["path"] = pathParam,
            }, 400);
        }

        var subdirs = new JsonArray();
        foreach (var dir in Directory.EnumerateDirectories(resolvedPath)
                     .Where(d => !Path.GetFileName(d).StartsWith('.'))
                     .OrderBy(d => Path.GetFileName(d).ToLowerInvariant()))
        {
            subdirs.Add(new JsonObject
            {
                ["name"] = Path.GetFileName(dir),
                ["path"] = ToRelative(dir, projectDir),
            });
        }

        var currentPath = ToRelative(resolvedPath, projectDir);

        // "" (the project root) is a valid parent, null means there's nowhere left to go up to
        string? parentPath = null;
        if (resolvedPath != projectDir)
        {
            var parentAbs = Path.GetDirectoryName(resolvedPath);
            if (parentAbs != null && ProjectsExtension.IsWithin(parentAbs, projectDir))
                parentPath = ToRelative(parentAbs, projectDir);
        }

        var userProjectsDir = Path.GetFullPath(Path.Combine(userPath, "projects"));
        var displayPath = ProjectsExtension.IsWithin(resolvedPath, userProjectsDir)
            ? "~/" + ToRelative(resolvedPath, userProjectsDir)
            : project != null
                ? $"~/{ProjectsExtension.GetProjectFolder(project)}"
                    + (currentPath.Length > 0 ? $"/{currentPath}" : "")
                : "~/" + Path.GetFileName(resolvedPath);

        return new JsonObject
        {
            ["currentPath"] = currentPath,
            ["displayPath"] = displayPath,
            ["parentPath"] = parentPath,
            ["subdirs"] = subdirs,
        };
    }

    /// <summary>Path relative to root, using '/' separators; the root itself is ""</summary>
    static string ToRelative(string path, string root)
    {
        var rel = Path.GetRelativePath(root, path);
        return rel == "." ? "" : rel.Replace(Path.DirectorySeparatorChar, '/');
    }

    // ── Publishing ──

    HttpClient CreateClient()
    {
        var client = Ctx.Feature.HttpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(Ctx.Limits.ClientTimeout);
        // llms-py sets this per-request; the remote content-negotiates on it (Vary: Accept) and
        // serves HTML — or a 302 to its login page — to clients that don't ask for JSON, which we
        // then fail to parse and silently drop the publishedUrl it returned.
        client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(MimeTypes.Json));
        return client;
    }

    [GeneratedRegex(@"/~cache/([^\s\)\""\'\>,]+)")]
    private static partial Regex CachePattern();

    /// <summary>Publish a thread: upload its referenced cache files + avatars, then the thread itself</summary>
    async Task<object?> PublishThreadAsync(ChatRequestContext req)
    {
        var user = req.UserName;
        var config = GetPublishConfig(user, obscure: false);
        var threadId = long.Parse(req.GetPathParam("id"));
        var thread = Ctx.Threads.GetThread(threadId, user)
            ?? throw new Exception("Thread not found");

        var apiKey = RequireApiKey(config);
        var baseUrl = BaseUrl(config);
        var profile = thread.GetObject("metadata").GetString("profile") ?? "default";

        using var client = CreateClient();

        // upload every /~cache/ file the thread references
        var cacheTails = CachePattern().Matches(thread.ToJsonString(ChatJson.Options))
            .Select(m => m.Groups[1].Value)
            .Distinct();
        foreach (var tail in cacheTails)
        {
            await UploadCacheFileAsync(client, apiKey, baseUrl, tail, user).ConfigAwait();
        }

        Ctx.Log.LogInformation("Publishing thread {ThreadId} '{Title}' to {Url}",
            threadId, thread.GetString("title"), baseUrl + "/publish/thread");

        var httpReq = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/publish/thread");
        httpReq.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        httpReq.Content = new StringContent(thread.ToJsonString(ChatJson.Options),
            System.Text.Encoding.UTF8, MimeTypes.Json);

        using var res = await client.SendAsync(httpReq).ConfigAwait();
        var text = await res.Content.ReadAsStringAsync().ConfigAwait();
        if (ChatJson.TryParseObject(text) is not { } data)
        {
            return new ChatResult { Status = (int)res.StatusCode, Text = text, ContentType = MimeTypes.PlainText };
        }

        var now = DateTime.Now;
        data["publishedAt"] = now.ToString("O");
        await Ctx.Threads.UpdateThreadAsync(threadId, new JsonObject
        {
            ["publishedAt"] = ChatDb.ToDateString(now),
            ["publishedUrl"] = data.GetString("publishedUrl"),
        }, user).ConfigAwait();

        await UploadAvatarsAsync(client, apiKey, baseUrl, config, user, profile).ConfigAwait();

        return ChatResult.Json(data, (int)res.StatusCode);
    }

    async Task UploadCacheFileAsync(HttpClient client, string apiKey, string baseUrl, string tail, string? user)
    {
        var filePath = Ctx.GetCachePath(tail);
        if (!File.Exists(filePath))
            return;

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath).ConfigAwait());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MimeTypes.GetMimeType(filePath));
        form.Add(fileContent, "file", Path.GetFileName(filePath));

        // merge the .info.json sidecar with any gallery media row for this hash
        var media = new JsonObject();
        var sidecarPath = Path.ChangeExtension(filePath, null) + ".info.json";
        if (File.Exists(sidecarPath) && ChatJson.TryParseObject(await File.ReadAllTextAsync(sidecarPath).ConfigAwait()) is { } sidecar)
        {
            media = sidecar;
        }
        var hash = Path.GetFileName(filePath).LeftPart('.');
        var medias = Ctx.Media.QueryMedia(new JsonObject { ["hash"] = hash }, user);
        if (medias.Count > 0)
        {
            foreach (var entry in medias[0])
                media[entry.Key] = entry.Value?.DeepClone();
        }
        if (media.GetString("type") is not { } type)
            return;
        if (type.Contains('/'))
            media["type"] = type.LeftPart('/');

        form.Add(new StringContent(media.ToJsonString(ChatJson.Options)), "info", Path.GetFileName(sidecarPath));

        var httpReq = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/publish/cache");
        httpReq.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        httpReq.Content = form;

        try
        {
            Ctx.Log.LogDebug("Uploading cache file {Path}", filePath);
            using var res = await client.SendAsync(httpReq).ConfigAwait();
            if (!res.IsSuccessStatusCode)
                Ctx.Log.LogError("Failed to upload cache file {Path}, status: {Status}", filePath, (int)res.StatusCode);
        }
        catch (Exception e)
        {
            Ctx.Log.LogError(e, "Exception during cache file upload {Path}", filePath);
        }
    }

    /// <summary>Upload the user's + profile's avatars once, remembering their published urls in the config</summary>
    async Task UploadAvatarsAsync(HttpClient client, string apiKey, string baseUrl, JsonObject config,
        string? user, string profile)
    {
        var avatars = config.GetObject("avatars");
        if (avatars == null)
        {
            avatars = new JsonObject();
            config["avatars"] = avatars;
        }

        var uploads = new List<(string Profile, string? Path)>();
        if (!avatars.ContainsKey("user"))
            uploads.Add(("user", FindAvatarFile(Ctx.GetUserPath(user), "avatar")));
        if (!avatars.ContainsKey(profile))
            uploads.Add((profile, FindAvatarFile(Ctx.GetUserPath(user), "agent")));

        foreach (var (avatarProfile, avatarPath) in uploads)
        {
            if (avatarPath == null)
                continue;
            try
            {
                using var form = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(avatarPath).ConfigAwait());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MimeTypes.GetMimeType(avatarPath));
                form.Add(fileContent, "file", Path.GetFileName(avatarPath));

                var httpReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/publish/avatar/{avatarProfile}");
                httpReq.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
                httpReq.Content = form;

                using var res = await client.SendAsync(httpReq).ConfigAwait();
                if (res.IsSuccessStatusCode
                    && ChatJson.TryParseObject(await res.Content.ReadAsStringAsync().ConfigAwait()) is { } avatarData
                    && avatarData.GetString("publishedUrl") is { } publishedUrl)
                {
                    avatars[avatarProfile] = publishedUrl;
                    SaveConfig(user, config);
                }
            }
            catch (Exception e)
            {
                Ctx.Log.LogError(e, "Failed to upload avatar {Profile}", avatarProfile);
            }
        }
    }

    static string? FindAvatarFile(string dir, string prefix) =>
        new[] { "webp", "png", "svg", "jpg", "jpeg" }
            .Select(ext => Path.Combine(dir, $"{prefix}.{ext}"))
            .FirstOrDefault(File.Exists);

    /// <summary>Publish a project's build folder (project.publish, relative to it) as a tar.gz</summary>
    async Task<object?> PublishProjectAsync(ChatRequestContext req)
    {
        var user = req.UserName;
        var name = req.GetPathParam("name");
        var project = Ctx.Projects.GetUserProjects(user).FirstOrDefault(p => p.GetString("name") == name)
            ?? throw new Exception("Project not found");

        var config = GetPublishConfig(user, obscure: false);
        var apiKey = RequireApiKey(config);
        var baseUrl = BaseUrl(config);

        // an empty (but present) publish deploys the project root
        if (!project.TryGetPropertyValue("publish", out var publishNode) || publishNode == null)
            throw new Exception("No publish directory configured for the project");

        var projectDir = ProjectsExtension.GetProjectDir(Ctx.GetUserPath(user), project);
        var publishDir = ProjectsExtension.SanitizePublishPath(project.GetString("publish"), projectDir);
        var resolvedDir = Path.GetFullPath(Path.Combine(projectDir, publishDir));
        if (!ProjectsExtension.IsWithin(resolvedDir, projectDir))
            throw new Exception("Publish directory must be within the project folder");
        if (!Directory.Exists(resolvedDir))
            throw new Exception($"Publish directory does not exist: {(publishDir.Length > 0 ? publishDir : "project root")}");

        using var tarStream = new MemoryStream();
        await using (var gzip = new GZipStream(tarStream, CompressionMode.Compress, leaveOpen: true))
        {
            await TarFile.CreateFromDirectoryAsync(resolvedDir, gzip, includeBaseDirectory: false).ConfigAwait();
        }

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(project.ToJsonString(ChatJson.Options)), "info", "info.json");
        var tarContent = new ByteArrayContent(tarStream.ToArray());
        tarContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/gzip");
        form.Add(tarContent, "file", $"{name}.tar.gz");

        var httpReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/publish/project/{name}");
        httpReq.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        httpReq.Content = form;

        Ctx.Log.LogDebug("Publishing project {Name} from {Dir}", name, resolvedDir);
        using var client = CreateClient();
        using var res = await client.SendAsync(httpReq).ConfigAwait();
        var text = await res.Content.ReadAsStringAsync().ConfigAwait();
        if (ChatJson.TryParseObject(text) is not { } data)
        {
            return new ChatResult { Status = (int)res.StatusCode, Text = text, ContentType = MimeTypes.PlainText };
        }

        if (res.IsSuccessStatusCode && data.GetString("publishedUrl") is { } publishedUrl)
        {
            project["publishedUrl"] = publishedUrl;
            var projects = Ctx.Projects.GetUserProjects(user);
            var arr = new JsonArray();
            foreach (var p in projects)
            {
                arr.Add((p.GetString("name") == name ? project : p).Clone());
            }
            var writePath = Path.Combine(Ctx.GetUserPath(user), "projects", "projects.json");
            Directory.CreateDirectory(Path.GetDirectoryName(writePath)!);
            await File.WriteAllTextAsync(writePath, arr.ToJsonString(ChatJson.Indented)).ConfigAwait();
        }
        return ChatResult.Json(data, (int)res.StatusCode);
    }

    /// <summary>Publish a single gallery media item + its cached file</summary>
    async Task<object?> PublishMediaAsync(ChatRequestContext req)
    {
        var user = req.UserName;
        var id = long.Parse(req.GetPathParam("id"));

        var rows = Ctx.Media.QueryMedia(new JsonObject { ["id"] = id }, user);
        if (rows.Count == 0)
            return ChatResult.Json(ChatJson.CreateErrorResponse("Media not found", "NotFound"), 404);
        var media = rows[0];

        var config = GetPublishConfig(user, obscure: false);
        var apiKey = RequireApiKey(config);
        var baseUrl = BaseUrl(config);

        var mediaUrl = media.GetString("url")
            ?? throw new Exception("Media URL not found");
        if (!mediaUrl.StartsWith("/~cache/"))
            throw new Exception("Invalid cache URL format");
        var filePath = Ctx.GetCachePath(mediaUrl["/~cache/".Length..]);
        if (!File.Exists(filePath))
            return ChatResult.Json(ChatJson.CreateErrorResponse($"Cached file not found: {filePath}", "NotFound"), 404);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(media.ToJsonString(ChatJson.Options)), "info", "info.json");
        var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath).ConfigAwait());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MimeTypes.GetMimeType(filePath));
        form.Add(fileContent, "file", Path.GetFileName(filePath));

        var httpReq = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/publish/media");
        httpReq.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        httpReq.Content = form;

        Ctx.Log.LogDebug("Publishing media {Id} from {Path}", id, filePath);
        using var client = CreateClient();
        using var res = await client.SendAsync(httpReq).ConfigAwait();
        var text = await res.Content.ReadAsStringAsync().ConfigAwait();
        if (ChatJson.TryParseObject(text) is not { } data)
        {
            return new ChatResult { Status = (int)res.StatusCode, Text = text, ContentType = MimeTypes.PlainText };
        }

        var now = DateTime.Now;
        data["publishedAt"] = now.ToString("O");
        await Ctx.Media.UpdateMediaAsync(id, new JsonObject
        {
            ["publishedAt"] = ChatDb.ToDateString(now),
            ["publishedUrl"] = data.GetString("publishedUrl"),
        }, user).ConfigAwait();

        return ChatResult.Json(data, (int)res.StatusCode);
    }
}
