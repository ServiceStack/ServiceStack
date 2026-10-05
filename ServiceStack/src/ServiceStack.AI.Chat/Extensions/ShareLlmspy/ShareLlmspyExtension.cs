using Microsoft.Extensions.Logging;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Share threads/media/projects on ai.llmspy.org (port of llms-py's "publish" extension).
/// Connection config is stored per user at App_Data/chat/user/&lt;user&gt;/share_llmspy/config.json.
/// </summary>
public partial class ShareLlmspyExtension : ChatExtension, IPublisherApi
{
    PublisherConfiguration configuration=null!;
    /// <summary>Optional host transport factory. It must prohibit redirects; default sockets transport does.</summary>
    public Func<HttpMessageHandler>? HttpHandlerFactory {get;set;}
    public bool Available=>Ctx!=null && !Ctx.Disabled && !Disabled;
    public JsonObject GetConfiguration(string user)=>configuration.Get(user,false);
    public Task<JsonNode?> SendAsync(string user,HttpMethod method,string path,JsonNode? body,bool authenticated,CancellationToken token)=>
        CreatePublisherClient(configuration.Get(user,false)).SendAsync(method,path,body,authenticated,token);
    public PublisherClient CreateClient(string user)=>Available?CreatePublisherClient(configuration.Get(user,false)):throw HttpError.ServiceUnavailable("Publishing is unavailable");
    PublisherClient CreatePublisherClient(JsonObject config)=>new(config,HttpHandlerFactory);
    static JsonObject RequireObject(JsonNode? value)=>value as JsonObject??throw new HttpError(502,"BadGateway","Publisher returned an unexpected response.");

    public ShareLlmspyExtension() : base("share_llmspy")
    {
        // Remote sharing is opt-in, independently of static exports and host configuration files.
        Disabled = true;
    }

    public override void Install(ExtensionContext ctx)
    {
        configuration=new PublisherConfiguration(ctx);
        ctx.Feature.PublisherApi=this;
        ctx.AddGet("config.json",req=>Task.FromResult<object?>(configuration.Get(req.UserName)));
        ctx.AddPost("disconnect",req=>Task.FromResult<object?>(configuration.Disconnect(req.UserName)));
        ctx.AddPost("config.json",async req=>configuration.Save(req.UserName,await req.GetJsonBodyAsync().ConfigAwait()));

        new ProjectOutputRoutes(ctx).Register();

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

    JsonObject GetPublishConfig(string? user,bool obscure=true)=>configuration.Get(user,obscure);
    string BaseUrl(JsonObject config)=>PublisherClient.Origin(config);
    string RequireApiKey(JsonObject config)=>!string.IsNullOrEmpty(config.GetString("apiKey"))?config.GetString("apiKey")!:throw HttpError.Unauthorized("Connect publisher account first.");

    // ── Publishing ──

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

        var client = CreatePublisherClient(config);

        // upload every /~cache/ file the thread references
        var cacheTails = CachePattern().Matches(thread.ToJsonString(ChatJson.Options))
            .Select(m => m.Groups[1].Value)
            .Distinct();
        foreach (var tail in cacheTails)
        {
            await UploadCacheFileAsync(client, apiKey, baseUrl, tail, user,req.Request.RequestAborted).ConfigAwait();
        }

        Ctx.Log.LogInformation("Publishing thread {ThreadId} '{Title}' to {Url}",
            threadId, thread.GetString("title"), baseUrl + "/publish/thread");

        var data=RequireObject(await client.SendAsync(HttpMethod.Post,"/publish/thread",thread,true,upload:true,req.Request.RequestAborted).ConfigAwait());

        var now = DateTime.Now;
        data["publishedAt"] = now.ToString("O");
        await Ctx.Threads.UpdateThreadAsync(threadId, new JsonObject
        {
            ["publishedAt"] = ChatDb.ToDateString(now),
            ["publishedUrl"] = data.GetString("publishedUrl"),
        }, user).ConfigAwait();

        await UploadAvatarsAsync(client, apiKey, baseUrl, config, user, profile,req.Request.RequestAborted).ConfigAwait();

        return ChatResult.Json(data);
    }

    async Task UploadCacheFileAsync(PublisherClient client, string apiKey, string baseUrl, string tail, string? user,CancellationToken token)
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

        try
        {
            await client.SendMultipartAsync("/publish/cache",form,token).ConfigAwait();
        }
        catch (Exception e) when(e is not OperationCanceledException) { Ctx.Log.LogWarning("Could not upload cache file {Path}: {Error}",filePath,ChatJson.ToErrorMessage(e)); }
    }

    /// <summary>Upload the user's + profile's avatars once, remembering their published urls in the config</summary>
    async Task UploadAvatarsAsync(PublisherClient client, string apiKey, string baseUrl, JsonObject config,
        string? user, string profile,CancellationToken token)
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

                var avatarData=RequireObject(await client.SendMultipartAsync("/publish/avatar/"+Uri.EscapeDataString(avatarProfile),form,token).ConfigAwait());
                if(avatarData.GetString("publishedUrl") is { } publishedUrl && configuration.SaveAvatar(user,config,avatarProfile,publishedUrl))
                    avatars[avatarProfile]=publishedUrl;
            }
            catch (Exception e) when(e is not OperationCanceledException)
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

        Ctx.Log.LogDebug("Publishing project {Name} from {Dir}", name, resolvedDir);
        var client=CreatePublisherClient(config);
        var data=RequireObject(await client.SendMultipartAsync("/publish/project/"+Uri.EscapeDataString(name),form,req.Request.RequestAborted).ConfigAwait());

        if (data.GetString("publishedUrl") is { } publishedUrl)
        {
            await Ctx.Projects.UpdatePublicationAsync(project,publishedUrl,user).ConfigAwait();
        }
        return ChatResult.Json(data);
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

        Ctx.Log.LogDebug("Publishing media {Id} from {Path}", id, filePath);
        var client=CreatePublisherClient(config);
        var data=RequireObject(await client.SendMultipartAsync("/publish/media",form,req.Request.RequestAborted).ConfigAwait());

        var now = DateTime.Now;
        data["publishedAt"] = now.ToString("O");
        await Ctx.Media.UpdateMediaAsync(id, new JsonObject
        {
            ["publishedAt"] = ChatDb.ToDateString(now),
            ["publishedUrl"] = data.GetString("publishedUrl"),
        }, user).ConfigAwait();

        return ChatResult.Json(data);
    }
}
