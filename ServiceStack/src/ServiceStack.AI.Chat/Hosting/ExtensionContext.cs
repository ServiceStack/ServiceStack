using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// Per-extension facade over ChatFeature's shared registries — the C# port of llms-py's
/// ExtensionContext (main.py:3730). Extension Install(ctx) methods mirror Python __install__(ctx).
/// </summary>
public class ExtensionContext(ChatFeature feature, string name)
{
    public ChatFeature Feature => feature;
    public string Name => name;
    public string ExtPrefix => $"/ext/{name}";

    /// <summary>Set to true inside Install() to disable the extension (e.g. voice without ffmpeg)</summary>
    public bool Disabled { get; set; }

    public ToolsExtension Tools => Feature.Tools;
    public McpExtension Mcp => Feature.Mcp;

    public ILogger Log => feature.Log;

    // ── Routing (paths auto-prefixed with /ext/<name>; a leading '/' escapes the prefix) ──

    string WebPath(string path) => string.IsNullOrEmpty(path)
        ? ExtPrefix
        : path.StartsWith('/') ? path : $"{ExtPrefix}/{path}";

    // routes require an authenticated request satisfying RequireAuth + RequiredRole unless they
    // opt out with allowAnon (only for what the UI needs before SignIn) - see RouteRegistry
    public void AddGet(string path, ChatRouteHandler handler, bool allowAnon = false) => feature.Routes.AddGet(WebPath(path), handler, allowAnon);
    public void AddPost(string path, ChatRouteHandler handler, bool allowAnon = false) => feature.Routes.AddPost(WebPath(path), handler, allowAnon);
    public void AddPut(string path, ChatRouteHandler handler, bool allowAnon = false) => feature.Routes.AddPut(WebPath(path), handler, allowAnon);
    public void AddDelete(string path, ChatRouteHandler handler, bool allowAnon = false) => feature.Routes.AddDelete(WebPath(path), handler, allowAnon);
    public void AddPatch(string path, ChatRouteHandler handler, bool allowAnon = false) => feature.Routes.AddPatch(WebPath(path), handler, allowAnon);

    /// <summary>
    /// Serve this extension's synced UI files (chat/ext/&lt;name&gt;/**) at /ext/&lt;name&gt;/{path}.
    /// Anonymous: the UI imports /ext/&lt;name&gt;/index.mjs before SignIn (the SignIn component itself
    /// is registered by the credentials extension's module). Registered after the extension's own
    /// routes, so this wildcard only serves paths its APIs didn't already claim.
    /// </summary>
    public void AddStaticFiles() =>
        feature.Routes.AddGet($"{ExtPrefix}/{{path:.*}}", ctx =>
            feature.ServeEmbeddedFileAsync(ctx, $"chat/ext/{name}", ctx.GetPathParam("path")), allowAnon: true);

    /// <summary>
    /// Read one of this extension's bundled files (chat/ext/&lt;name&gt;/&lt;path&gt;), e.g. the AI
    /// prompts and starter templates synced from llms-py alongside its UI.
    /// </summary>
    public string? GetBundledText(string path) =>
        HostContext.VirtualFileSources.GetFile($"chat/ext/{name}/{path}")?.ReadAllText();

    /// <summary>
    /// Register in the /ext list so the UI dynamically imports /ext/&lt;name&gt;/index.mjs
    /// (a leading '/' escapes the prefix, e.g. "/custom/index.mjs")
    /// </summary>
    public void RegisterUiExtension(string index = "index.mjs") =>
        feature.UiExtensions.Add(new JsonObject { ["id"] = name, ["path"] = WebPath(index) });

    public void AddImportMaps(Dictionary<string, string> maps)
    {
        foreach (var entry in maps)
            feature.ImportMaps[entry.Key] = entry.Value;
    }

    public void AddIndexHeader(string html) => feature.IndexHeaders.Add(html);
    public void AddIndexFooter(string html) => feature.IndexFooters.Add(html);

    // ── Chat pipeline filters ──

    public void RegisterChatRequestFilter(Func<JsonObject, ChatContext, Task> handler) => feature.Filters.ChatRequestFilters.Add(handler);
    public void RegisterChatToolFilter(Func<JsonObject, ChatContext, Task> handler) => feature.Filters.ChatToolFilters.Add(handler);
    public void RegisterChatApprovalFilter(Func<JsonObject, ChatContext, Task> handler) => feature.Filters.ChatApprovalFilters.Add(handler);
    public void RegisterChatStatusFilter(Func<string, ChatContext, Task> handler) => feature.Filters.ChatStatusFilters.Add(handler);
    public void RegisterChatResponseFilter(Func<JsonObject, ChatContext, Task> handler) => feature.Filters.ChatResponseFilters.Add(handler);
    public void RegisterChatErrorFilter(Func<Exception, ChatContext, Task> handler) => feature.Filters.ChatErrorFilters.Add(handler);
    public void RegisterCacheSavedFilter(Action<CacheSavedContext> handler) => feature.Filters.CacheSavedFilters.Add(handler);
    public void RegisterSetupUserHandler(Func<IRequest, Task> handler) => feature.Filters.SetupUserHandlers.Add(handler);
    public void RegisterShutdownHandler(Action handler) => feature.Filters.ShutdownHandlers.Add(handler);

    // ── Tools ──

    public void RegisterTool(JsonObject definition, ChatToolHandler handler, string? group = null,
        ChatToolApprovalHandler? approvalHandler = null, JsonObject? outputSchema = null,
        ToolSafety safety = ToolSafety.Auto) =>
        feature.Tools.Register(new ChatTool
        {
            Definition = definition,
            Handler = handler,
            ApprovalHandler = approvalHandler,
            OutputSchema = outputSchema,
            Safety = safety,
            Group = group ?? name,
        });

    /// <summary>
    /// Register a ServiceStack Command as a tool: the Command's request type becomes the tool's
    /// JSON Schema and calls run through CommandsFeature, so a tool call is executed, validated,
    /// retried and logged exactly like any other command. Name it with [Tool(Name)], describe it
    /// with [Description] and [Tool(WhenToUse)].
    /// </summary>
    public void RegisterTool<TCommand>(string? group = null) where TCommand : IAsyncCommand =>
        RegisterTool(typeof(TCommand), group);

    /// <summary>Register a Command as a tool, for a Command type only known at runtime</summary>
    public void RegisterTool(Type commandType, string? group = null) =>
        feature.Tools.Register(CommandTool.Create(commandType, feature, group ?? name));

    public JsonObject? GetToolDefinition(string toolName) => feature.Tools.GetToolDefinition(toolName);

    // ── Providers ──

    /// <summary>Register a provider type resolvable by its npm sdk id (port of ctx.add_provider)</summary>
    public void AddProvider(string sdk, Func<ChatProvider> factory) => feature.ProviderTypes[sdk] = factory;

    public Dictionary<string, ChatProvider> GetProviders() => feature.Providers;

    // ── Auth / users ──

    public bool IsAuthEnabled => feature.ChatAuth.IsEnabled;
    /// <summary>True for Admins, and for everyone when auth is disabled (Python: ctx.is_admin)</summary>
    public bool IsAdmin(IRequest request) => feature.ChatAuth.IsAdmin(request);
    public string? GetUserName(IRequest request) => feature.ChatAuth.GetUserName(request);
    public string? AssertUserName(IRequest request) => feature.ChatAuth.AssertUserName(request);
    public (bool IsAuthenticated, JsonObject? Session) CheckAuth(IRequest request) => feature.ChatAuth.CheckAuth(request);

    // ── Paths & files (all under App_Data/chat) ──

    public ChatAppData AppData => feature.AppData;
    public string GetHomePath(string path = "") => feature.AppData.GetHomePath(path);
    public string GetCachePath(string path = "") => feature.AppData.GetCachePath(path);
    public string GetUserPath(string? user = null) => feature.AppData.GetUserPath(user);
    public JsonObject GetUserPrefs(string? user) => feature.AppData.GetUserPrefs(user);
    public void SetUserPref(string key, JsonNode? value, string? user) => feature.AppData.SetUserPref(key, value, user);

    // ── Config ──

    public JsonObject Config => feature.Config;
    public JsonObject? GetConfigDefaults() => feature.Config.GetObject("defaults");
    public ChatLimits Limits => feature.Limits;

    // ── Chat helpers ──

    public Task<JsonObject> ChatCompletionAsync(JsonObject chat, ChatContext context) =>
        feature.ChatCompletionAsync(chat, context);

    public string NextLoadingMessage() => feature.NextLoadingMessage();

    public string? LastUserPrompt(JsonObject chat) => ChatMessages.LastUserPrompt(chat);
    public string? ChatToSystemPrompt(JsonObject chat) => ChatMessages.ChatToSystemPrompt(chat);
    public string? ChatToAspectRatio(JsonObject chat) => ChatMessages.ChatToAspectRatio(chat);
    public JsonObject ChatResponseToMessage(JsonObject response) => ChatMessages.ChatResponseToMessage(response);

    /// <summary>Write bytes to the content-addressed cache, firing the cache_saved filters</summary>
    public JsonObject SaveToCache(byte[] content, string filename, string mimeType, string? user,
        JsonObject? extraInfo = null) => feature.SaveToCache(content, filename, mimeType, user, extraInfo);

    // ── Pluggable cross-extension APIs ──

    public IThreadApi Threads
    {
        get => feature.ThreadApi;
        set => feature.ThreadApi = value;
    }
    public IMediaApi Media
    {
        get => feature.MediaApi;
        set => feature.MediaApi = value;
    }
    public IProjectsApi Projects
    {
        get => feature.ProjectsApi;
        set => feature.ProjectsApi = value;
    }

    // ── Sandboxing (computer/core_tools/projects) ──

    public void AddAllowedDirectory(string path, string? user = null) => feature.AddAllowedDirectory(path, user);
    public void SetAllowedDirectories(IEnumerable<string> directories, string? user = null) => feature.SetAllowedDirectories(directories, user);
    public string? ResolveDirectory(string dir) => feature.ResolveDirectory(dir);
    public List<string> GetAllowedDirectories(string? user = null) => feature.AllowedDirectories.GetValueOrDefault(user ?? "default") ?? [];
    public List<string> ResolveAllowedDirectories(string? user = null) => feature.ResolveAllowedDirectories(user);

    public JsonNode? GetUserPref(string key, string? user = null) => feature.AppData.GetUserPrefs(user)[key]?.DeepClone();
}
