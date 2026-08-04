using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// Lets the LLM discover and call the App's own ServiceStack APIs, opted in with [Tool] or
/// ChatFeature.ToolsConfig.ApiTools. Requires ToolsConfig.EnableApiTools.
/// <para>
/// Three tools rather than one per API: an App's whole API surface is far too large to keep in
/// context (270 APIs is ~156K tokens of schema), so only the search index is ever loaded eagerly,
/// and only the APIs an Agent actually uses cost it their schema.
/// </para>
/// </summary>
public class ApiToolsExtension() : ChatExtension("api_tools")
{
    /// <summary>
    /// Which APIs EnableApiTools exposes. [Tool] APIs are always included; use IncludeTags to
    /// expose APIs in bulk. Shared with any other Agent transport (e.g. MCP) the host adds.
    /// </summary>
    /// <summary>Expose every API with these [Tag]s, without annotating each Request DTO with [Tool]</summary>
    public List<string> IncludeTags { get; set; } = [];
    /// <summary>Expose these Request DTOs by name, for APIs you can't annotate with [Tool]</summary>
    public List<string> IncludeTypes { get; set; } = [];
    /// <summary>Never expose these Request DTOs, whatever else includes them</summary>
    public List<string> ExcludeTypes { get; set; } = [];
    /// <summary>Rows returned when neither the Agent nor [Tool(Take)] specifies a limit</summary>
    public int DefaultTake { get; set; } = 25;
    /// <summary>Maximum rows an Agent can ask for, whatever it requests</summary>
    public int MaxTake { get; set; } = 100;

    /// <summary>Result JSON longer than this is truncated — one query mustn't eat the context window</summary>
    public int MaxResultLength { get; set; } = 32 * 1024;

    ExtensionContext ctx = null!;
    ApiToolRegistry registry = null!;
    public ApiToolRegistry? Registry => registry;

    public override void Install(ExtensionContext ctx)
    {
        this.ctx = ctx;
        if (!ctx.Tools.EnableApiTools)
        {
            Disabled = true;
            return;
        }
        registry = new ApiToolRegistry(new()
        {
            IncludeTags = IncludeTags,
            IncludeTypes = IncludeTypes,
            ExcludeTypes = ExcludeTypes,
            DefaultTake = DefaultTake,
            MaxTake = MaxTake,
        });

        const string group = "api_tools";

        ctx.RegisterTool(ToolDef("api_search",
            "Search the APIs of the App you are running inside, by keyword and/or tag. "
            + "Returns one line per API: name, tags and what it's for. Call api_describe next to "
            + "get an API's arguments. Prefer this over guessing an API name.",
            new JsonObject
            {
                ["query"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "Words to search for, e.g. 'customer orders'. Omit to list all APIs in a tag.",
                },
                ["tag"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "Only return APIs with this tag",
                },
                ["take"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "Max APIs to return (default 20)",
                },
            }), SearchAsync, group);

        ctx.RegisterTool(ToolDef("api_describe",
            "Get the JSON Schema of one or more APIs: their arguments, which are required, and "
            + "example payloads. Call this before api_call unless you already know the arguments.",
            new JsonObject
            {
                ["names"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "string" },
                    ["description"] = "API names returned by api_search",
                },
            }, required: ["names"]), DescribeAsync, group);

        ctx.RegisterTool(ToolDef("api_call",
            "Call one of this App's APIs and return its JSON response. Runs as the signed-in user, "
            + "so it can only do what they're allowed to do. Results may be truncated — filter or "
            + "page rather than asking for everything.",
            new JsonObject
            {
                ["name"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "API name from api_search",
                },
                ["args"] = new JsonObject
                {
                    ["type"] = "object",
                    ["description"] = "Arguments matching the API's schema from api_describe",
                },
            }, required: ["name"]), CallAsync, group);
    }

    Task<object?> SearchAsync(JsonObject args, ChatContext context)
    {
        if (RequestOf(context) is not { } req)
            return Task.FromResult<object?>(NoRequestError);

        var take = args.GetInt("take") ?? 20;
        var results = registry.Search(req, args.GetString("query"), args.GetString("tag"), take);
        if (results.Count == 0)
        {
            var tags = registry.GetTags(req);
            return Task.FromResult<object?>(tags.Count > 0
                ? $"No APIs matched. Available tags: {string.Join(", ", tags)}"
                : "No APIs are available to you.");
        }

        var sb = StringBuilderCache.Allocate();
        sb.AppendLine($"{results.Count} API(s):");
        foreach (var tool in results)
        {
            sb.AppendLine(tool.ToSummaryLine());
        }
        return Task.FromResult<object?>(StringBuilderCache.ReturnAndFree(sb));
    }

    Task<object?> DescribeAsync(JsonObject args, ChatContext context)
    {
        if (RequestOf(context) is not { } req)
            return Task.FromResult<object?>(NoRequestError);

        var names = args["names"] is JsonArray array
            ? array.Select(x => x?.ToString()).Where(x => !string.IsNullOrEmpty(x)).ToList()
            : args.GetString("names") is { } single ? [single] : new List<string?>();
        if (names.Count == 0)
            return Task.FromResult<object?>("Error: 'names' is required");

        var to = new JsonArray();
        foreach (var name in names)
        {
            var tool = registry.GetTool(name!, req);
            if (tool == null)
            {
                to.Add(new JsonObject { ["name"] = name, ["error"] = "Not found or not available to you" });
                continue;
            }

            var describe = new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Summary,
                ["arguments"] = ChatJson.Parse(tool.InputSchema.ToJson()),
                ["safety"] = tool.Safety.ToString().ToLower(),
            };
            if (!string.IsNullOrEmpty(tool.WhenToUse))
                describe["whenToUse"] = tool.WhenToUse;
            if (!string.IsNullOrEmpty(tool.Notes))
                describe["notes"] = tool.Notes;
            if (tool.Examples.Count > 0)
                describe["examples"] = new JsonArray(tool.Examples.Select(x => (JsonNode)x).ToArray());
            if (tool.RequiresApproval || tool.Safety == ToolSafety.Destructive)
                describe["requiresApproval"] = true;
            to.Add(describe);
        }
        return Task.FromResult<object?>(to);
    }

    async Task<object?> CallAsync(JsonObject args, ChatContext context)
    {
        if (RequestOf(context) is not { } req)
            return NoRequestError;

        var name = args.GetString("name");
        if (string.IsNullOrEmpty(name))
            return "Error: 'name' is required";

        var tool = registry.GetTool(name!, req);
        if (tool == null)
            return $"Error: API '{name}' not found or not available to you. Use api_search to find one.";

        var argsJson = args["args"] is JsonObject dtoArgs ? dtoArgs.ToJsonString(ChatJson.Options) : null;
        Log.LogInformation("api_call {Api} as {User}", tool.RequestType, context.User);

        var response = await registry.ExecuteAsync(tool, argsJson, req).ConfigAwait();
        var json = response.ToJson() ?? "null";
        return json.Length > MaxResultLength
            ? json[..MaxResultLength] + $"\n...[truncated at {MaxResultLength} chars, narrow the query]"
            : json;
    }

    /// <summary>
    /// API tools can only run on behalf of a request. Without one there's no user to run as, and
    /// executing anyway would run the App's APIs unauthenticated.
    /// </summary>
    static IRequest? RequestOf(ChatContext context) => context.Request;

    const string NoRequestError =
        "Error: API tools are unavailable in this context (no authenticated request to act on behalf of)";

    static JsonObject ToolDef(string name, string description, JsonObject properties, string[]? required = null)
    {
        var parameters = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
        };
        if (required is { Length: > 0 })
            parameters["required"] = new JsonArray(required.Select(x => (JsonNode)x).ToArray());
        return new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = name,
                ["description"] = description,
                ["parameters"] = parameters,
            },
        };
    }
}
