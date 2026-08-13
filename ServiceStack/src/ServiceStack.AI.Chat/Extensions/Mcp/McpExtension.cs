using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// Exposes this App's Chat tools to external AI Agents (Claude Code, Cursor, VS Code, …) as an
/// MCP Server at {RoutePrefix}/mcp, opted in with ChatFeature.Mcp.
/// <para>
/// Streamable HTTP transport, run stateless: every request is a self-contained JSON-RPC POST
/// answered with JSON. No SSE stream and no Mcp-Session-Id, which a tools-only server doesn't
/// need — there's no server-initiated message to deliver and nothing to keep between calls.
/// </para>
/// <para>
/// Tools run as the caller: MCP Clients authenticate with a Bearer API Key, which
/// ChatFeature.OnRequestAsync resolves onto the request, so api_tools execute against the App's
/// APIs with that user's access rather than as the App itself.
/// </para>
/// </summary>
public class McpExtension() : ChatExtension("mcp")
{
    /// <summary>
    /// Which Chat tools external AI Agents can use over MCP at {RoutePrefix}/mcp. Nothing is
    /// exposed until ToolGroups/Tools names something.
    /// </summary>
    /// <summary>Tool groups to expose, e.g. "api_tools", "core_tools". Empty disables the endpoint.</summary>
    public List<string> ToolGroups { get; set; } = ["api_tools"];

    /// <summary>Individual tools to expose, in addition to whole <see cref="ToolGroups"/></summary>
    public List<string> Tools { get; set; } = [];

    /// <summary>Server name reported to MCP Clients in initialize</summary>
    public string? ServerName { get; set; }

    /// <summary>Server version reported to MCP Clients (defaults to the ServiceStack version)</summary>
    public string? ServerVersion { get; set; }

    /// <summary>Optional usage hint Clients can add to their system prompt</summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Reject tools that require ServiceStack's interactive approval UI, which MCP clients cannot
    /// present. Disable only when the MCP client is trusted to confirm write/destructive calls.
    /// </summary>
    public bool RejectToolsRequiringApproval { get; set; } = true;

    /// <summary>
    /// Largest image/audio result inlined as base64 in a tool result. Larger resources are
    /// returned as a link instead — an Agent can't stream a 40MB wav through its context.
    /// </summary>
    public int MaxInlineResourceBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>Whether the host has opted in to exposing anything over MCP</summary>
    public bool IsEnabled => ToolGroups.Count > 0 || Tools.Count > 0;

    /// <summary>ToolGroups + Tools as a <see cref="ToolRegistry.SelectTools"/> selector</summary>
    public string ToolSelector => ToolGroups.Contains("all") || Tools.Contains("all")
        ? "all"
        : string.Join(",", ToolGroups.Union(Tools));

    
    /// <summary>Protocol revision used when a Client asks for one we don't know</summary>
    public const string LatestProtocolVersion = "2025-06-18";

    /// <summary>Revisions we'll negotiate down to, newest first</summary>
    public static string[] SupportedProtocolVersions { get; set; } =
        [LatestProtocolVersion, "2025-03-26", "2024-11-05"];

    // JSON-RPC 2.0 error codes
    public const int ParseError = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams = -32602;
    public const int InternalError = -32603;
    
    public string GetServerName() => ServerName ?? HostContext.ServiceName ?? "servicestack-ai-chat";
    public string GetServerVersion() => ServerVersion ?? Env.VersionString;

    public override void Install(ExtensionContext ctx)
    {
        if (ctx.Feature.Tools.Disabled)
        {
            ctx.Disabled = true;
            return;
        }

        ctx.AddGet("", req =>
        {
            var selectedTools = SelectedTools();
            var toolNames = selectedTools.Select(x => x.Name).ToList();
            var mcpUrl = ctx.Feature.ResolveClientUrl("/mcp") ?? "/mcp";

            var apiToolsExt = ctx.Feature.ApiTools;
            var apiTags = apiToolsExt?.IncludeTags;
            var apiTools = apiToolsExt?.Registry?.GetAll().Select(x => x.Name).ToList();

            var res = new JsonObject
            {
                ["serverName"] = GetServerName(),
                ["serverVersion"] = GetServerVersion(),
                ["instructions"] = Instructions ?? "",
                ["isEnabled"] = IsEnabled,
                ["url"] = mcpUrl,
                ["toolGroups"] = new JsonArray(ToolGroups.Select(x => (JsonNode)x!).ToArray()),
                ["tools"] = new JsonArray(toolNames.Select(x => (JsonNode)x!).ToArray()),
            };
            if (apiTags is { Count: > 0 })
                res["apiTags"] = new JsonArray(apiTags.Select(x => (JsonNode)x!).ToArray());
            if (apiTools is { Count: > 0 })
                res["apiTools"] = new JsonArray(apiTools.Select(x => (JsonNode)x!).ToArray());

            return Task.FromResult<object?>(res);
        });

        // '/' escapes the /ext/<name> prefix: MCP Clients are configured with this URL by hand,
        // so it's worth keeping short (default /chat/mcp)
        ctx.AddPost("/mcp", HandleAsync);
        // the spec requires 405 from a server that doesn't offer the optional GET SSE stream,
        // and DELETE only applies to sessions, which a stateless server doesn't issue
        ctx.AddGet("/mcp", _ => Task.FromResult<object?>(MethodNotAllowed()));
        ctx.AddDelete("/mcp", _ => Task.FromResult<object?>(MethodNotAllowed()));

        Log.LogInformation("MCP Server enabled at {Path}/mcp exposing: {Tools}",
            ctx.Feature.RoutePrefix, ToolSelector);
    }

    async Task<object?> HandleAsync(ChatRequestContext req)
    {
        if (Ctx.Feature.ChatAuth.IsEnabled && req.UserName == null)
            return Unauthorized();

        JsonNode? body;
        try
        {
            body = await req.GetJsonNodeBodyAsync().ConfigAwait();
        }
        catch (JsonException e)
        {
            return ChatResult.Json(ErrorResponse(null, ParseError, e.Message));
        }

        // 2025-06-18 dropped JSON-RPC batching, but Clients on earlier revisions can still send an array
        if (body is JsonArray batch)
        {
            var responses = new JsonArray();
            foreach (var message in batch)
            {
                if (await HandleMessageAsync(message as JsonObject, req).ConfigAwait() is { } response)
                    responses.Add(response);
            }
            return responses.Count > 0 ? ChatResult.Json(responses) : Accepted();
        }

        var result = await HandleMessageAsync(body as JsonObject, req).ConfigAwait();
        // notifications and responses have nothing to reply with
        return result != null ? ChatResult.Json(result) : Accepted();
    }

    /// <summary>Handle one JSON-RPC message. Returns null when the message doesn't want a response.</summary>
    async Task<JsonObject?> HandleMessageAsync(JsonObject? message, ChatRequestContext req)
    {
        if (message == null)
            return ErrorResponse(null, InvalidRequest, "Invalid Request");

        var method = message.GetString("method");
        if (method == null) // a response to something we sent, which stateless servers never send
            return null;

        // a request has an id and expects a response; a notification has neither
        var isNotification = !message.ContainsKey("id");
        var id = message["id"];
        var args = message.GetObject("params") ?? new JsonObject();

        try
        {
            var result = method switch
            {
                "initialize" => Initialize(args),
                "ping" => new JsonObject(),
                "tools/list" => ListTools(),
                "tools/call" => await CallToolAsync(args, req).ConfigAwait(),
                _ when method.StartsWith("notifications/") => null,
                _ => throw new McpException(MethodNotFound, $"Method not found: {method}"),
            };
            return isNotification ? null : SuccessResponse(id, result ?? new JsonObject());
        }
        catch (McpException e)
        {
            return isNotification ? null : ErrorResponse(id, e.Code, e.Message);
        }
        catch (Exception e)
        {
            Log.LogError(e, "MCP {Method} failed: {Message}", method, e.Message);
            return isNotification ? null : ErrorResponse(id, InternalError, ChatJson.ToErrorMessage(e));
        }
    }

    JsonObject Initialize(JsonObject args)
    {
        // agree on what the Client asked for when we speak it, otherwise answer in ours and let
        // the Client decide whether it can continue
        var requested = args.GetString("protocolVersion");
        var version = requested != null && SupportedProtocolVersions.Contains(requested)
            ? requested
            : LatestProtocolVersion;

        var to = new JsonObject
        {
            ["protocolVersion"] = version,
            ["capabilities"] = new JsonObject
            {
                // listChanged:false — the tool list is built at startup and never changes
                ["tools"] = new JsonObject { ["listChanged"] = false },
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = GetServerName(),
                ["version"] = GetServerVersion(),
            },
        };
        if (!string.IsNullOrEmpty(Instructions))
            to["instructions"] = Instructions;
        return to;
    }

    /// <summary>Only the tools the host exposed, never the whole registry</summary>
    List<ChatTool> SelectedTools() => Ctx.Feature.Tools.SelectTools(ToolSelector);

    JsonObject ListTools()
    {
        var tools = new JsonArray();
        foreach (var tool in SelectedTools())
        {
            if (ToMcpTool(tool) is { } def)
                tools.Add(def);
        }
        return new JsonObject { ["tools"] = tools };
    }

    /// <summary>OpenAI function definition → MCP Tool: the same JSON Schema under different keys</summary>
    static JsonObject? ToMcpTool(ChatTool tool)
    {
        var fn = tool.Definition.GetObject("function");
        var name = fn.GetString("name");
        if (name == null)
            return null;

        var to = new JsonObject { ["name"] = name };
        if (fn.GetString("description") is { } description)
            to["description"] = description;
        // MCP requires an object schema; tools with no arguments still need an empty one
        to["inputSchema"] = fn.GetObject("parameters")?.Clone()
            ?? new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() };
        if (tool.OutputSchema != null)
            to["outputSchema"] = tool.OutputSchema.Clone();
        // hints Clients use to decide what to auto-approve, for tools that declare their safety
        if (Annotations(tool.Safety) is { } annotations)
            to["annotations"] = annotations;
        return to;
    }

    static JsonObject? Annotations(ToolSafety safety) => safety switch
    {
        ToolSafety.ReadOnly => new JsonObject { ["readOnlyHint"] = true },
        ToolSafety.Write => new JsonObject { ["readOnlyHint"] = false, ["destructiveHint"] = false },
        ToolSafety.Destructive => new JsonObject { ["readOnlyHint"] = false, ["destructiveHint"] = true },
        _ => null,
    };

    async Task<JsonObject> CallToolAsync(JsonObject args, ChatRequestContext req)
    {
        var name = args.GetString("name");
        if (string.IsNullOrEmpty(name))
            throw new McpException(InvalidParams, "'name' is required");

        // resolved against the exposed selection, so naming an unexposed tool (e.g. run_bash)
        // can't reach it
        var tool = SelectedTools().FirstOrDefault(x => x.Name == name);
        if (tool == null)
            throw new McpException(InvalidParams, $"Tool '{name}' is not available");

        // only pass args declared in the tool's schema
        var toolArgs = new JsonObject();
        var properties = tool.Definition.GetObject("function").GetObject("parameters").GetObject("properties");
        foreach (var entry in args.GetObject("arguments") ?? new JsonObject())
        {
            if (properties == null || properties.ContainsKey(entry.Key))
                toolArgs[entry.Key] = entry.Value?.DeepClone();
        }

        Log.LogInformation("MCP tools/call {Tool} as {User}", name, req.UserName);
        var context = new ChatContext { User = req.UserName, Request = req.Request };
        if (RejectToolsRequiringApproval)
            context.Items[ChatContext.RejectToolsRequiringApproval] = true;
        // tool errors come back as result text (ExecToolAsync never throws), which is what an
        // Agent wants to read anyway — isError is reserved for what it couldn't attempt
        var (text, resources) = await Ctx.Feature.ExecToolAsync(name!, toolArgs, context).ConfigAwait();

        var content = new JsonArray();
        if (!string.IsNullOrEmpty(text))
            content.Add(new JsonObject { ["type"] = "text", ["text"] = text });
        foreach (var resource in resources)
        {
            if (ToMcpContent(resource, req.Request) is { } part)
                content.Add(part);
        }
        if (content.Count == 0)
            content.Add(new JsonObject { ["type"] = "text", ["text"] = "" });

        var result = new JsonObject { ["content"] = content };
        if (ChatJson.TryParseObject(text) is { } structured)
            result["structuredContent"] = structured;
        return result;
    }

    /// <summary>
    /// Chat resource part → MCP content. Media is inlined as base64 where it's small enough:
    /// an external Agent has no session with this App, so a link to its cache may be unfetchable.
    /// </summary>
    JsonObject? ToMcpContent(JsonObject resource, IRequest req)
    {
        var type = resource.GetString("type");
        (string? url, string? filename, string? kind) = type switch
        {
            "image_url" => (resource.GetObject("image_url").GetString("url"), null, "image"),
            "audio_url" => (resource.GetObject("audio_url").GetString("url"), null, "audio"),
            "file" => (resource.GetObject("file").GetString("file_data"),
                resource.GetObject("file").GetString("filename"), null),
            _ => ((string?)null, (string?)null, (string?)null),
        };
        if (string.IsNullOrEmpty(url))
            return null;

        var mimeType = MimeTypes.GetMimeType(filename ?? url);
        if (kind != null && TryReadCached(url!) is { } bytes)
        {
            return new JsonObject
            {
                ["type"] = kind,
                ["data"] = Convert.ToBase64String(bytes),
                ["mimeType"] = mimeType,
            };
        }

        return new JsonObject
        {
            ["type"] = "resource_link",
            ["uri"] = AbsoluteUrl(url!, req),
            ["name"] = filename ?? url!.LastRightPart('/'),
            ["mimeType"] = mimeType,
        };
    }

    /// <summary>Read a {RoutePrefix}/~cache/ URL back off disk, or null if it's too big to inline</summary>
    byte[]? TryReadCached(string url)
    {
        const string cachePrefix = "/~cache/";
        if (!url.StartsWith(cachePrefix, StringComparison.OrdinalIgnoreCase))
            return null;
        try
        {
            var path = Ctx.AppData.GetCachePath(url[cachePrefix.Length..]);
            if (!File.Exists(path) || new FileInfo(path).Length > MaxInlineResourceBytes)
                return null;
            return File.ReadAllBytes(path);
        }
        catch (Exception e)
        {
            Log.LogDebug(e, "Could not read cached resource {Url}", url);
            return null;
        }
    }

    string AbsoluteUrl(string url, IRequest req) =>
        req.GetBaseUrl().CombineWith(Ctx.Feature.RoutePrefix, url);

    // ── JSON-RPC + HTTP plumbing ──

    static JsonObject SuccessResponse(JsonNode? id, JsonObject result) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["result"] = result,
    };

    static JsonObject ErrorResponse(JsonNode? id, int code, string message) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id?.DeepClone(),
        ["error"] = new JsonObject
        {
            ["code"] = code,
            ["message"] = message,
        },
    };

    /// <summary>A batch of notifications has no response body to return</summary>
    static ChatResult Accepted() => new() { Status = 202, ContentType = MimeTypes.Json, Text = "" };

    static ChatResult MethodNotAllowed() => new()
    {
        Status = 405,
        ContentType = MimeTypes.PlainText,
        Text = "405: Method Not Allowed",
        Headers = new Dictionary<string, string> { ["Allow"] = "POST" },
    };

    ChatResult Unauthorized() => new()
    {
        Status = 401,
        ContentType = MimeTypes.Json,
        Text = Ctx.Feature.ErrorAuthRequired().ToJsonString(ChatJson.Options),
        Headers = new Dictionary<string, string> { ["WWW-Authenticate"] = "Bearer" },
    };
}

/// <summary>A failure to report as a JSON-RPC error rather than as a tool result</summary>
public class McpException(int code, string message) : Exception(message)
{
    public int Code => code;
}
