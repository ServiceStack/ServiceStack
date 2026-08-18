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
            }), SearchAsync, group, outputSchema: SearchOutputSchema(), safety: ToolSafety.ReadOnly);

        ctx.RegisterTool(ToolDef("api_describe",
            "Get the API JSON Schema for one or more Request DTOs, including their arguments, "
            + "required fields, validation, UI metadata, safety and examples. Call this before "
            + "api_call unless you already know the arguments.",
            new JsonObject
            {
                ["names"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "string" },
                    ["description"] = "API names returned by api_search",
                },
            }, required: ["names"]), DescribeAsync, group,
            outputSchema: DescribeOutputSchema(), safety: ToolSafety.ReadOnly);

        ctx.RegisterTool(ToolDef("api_call",
            "Call one of this App's APIs and return its JSON response. Runs as the signed-in user, "
            + "so it can only do what they're allowed to do. Write and destructive calls pause for "
            + "the user to review and confirm their arguments. Results may be truncated — filter "
            + "or page rather than asking for everything.",
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
                ["confirmationToken"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "Confirmation token returned by a previous requires_confirmation response for write/destructive operations.",
                },
            }, required: ["name"]), CallAsync, group, ApprovalAsync,
            outputSchema: CallOutputSchema(), safety: ToolSafety.Write);

        if (ctx.Feature.ChatDb != null)
        {
            ctx.RegisterUiExtension("/custom/ApiApprovalForm.mjs");
            var approvals = new ApiToolApprovalCoordinator(this, ctx);
            approvals.Install();
            ctx.Feature.ToolApprovalCoordinator = approvals;
        }
    }

    Task<object?> SearchAsync(JsonObject args, ChatContext context)
    {
        if (RequestOf(context) is not { } req)
            return Task.FromResult<object?>(NoRequestError);

        var take = args.GetInt("take") ?? 20;
        var tag = args.GetString("tag");
        var isMcp = context.Items.ContainsKey(ChatContext.McpTransport);
        var results = registry.Search(req, args.GetString("query"), tag, take);
        if (results.Count == 0)
        {
            var tags = registry.GetTags(req);
            return Task.FromResult<object?>(new JsonObject
            {
                ["status"] = "no_match",
                ["count"] = 0,
                ["apis"] = new JsonArray(),
                ["availableTags"] = new JsonArray(tags.Select(x => (JsonNode)x).ToArray()),
                ["suggestedApis"] = new JsonArray(registry.Search(req, null, tag, 5)
                    .Select(x => (JsonNode)x.Name).ToArray()),
                ["next"] = "Try broader user vocabulary, an available tag, or one of suggestedApis",
            });
        }

        return Task.FromResult<object?>(new JsonObject
        {
            ["status"] = "success",
            ["count"] = results.Count,
            ["apis"] = new JsonArray(results.Select(tool => (JsonNode)new JsonObject
            {
                ["name"] = tool.Name,
                ["request"] = tool.RequestType,
                ["summary"] = isMcp ? tool.McpSummary : tool.Summary,
                ["tags"] = new JsonArray(tool.Tags.Select(x => (JsonNode)x).ToArray()),
                ["safety"] = tool.Safety.ToString().ToLowerInvariant(),
                ["method"] = tool.Method,
                ["route"] = tool.Route,
            }).ToArray()),
            ["next"] = "Call api_describe with the names of the APIs you intend to use",
        });
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

        var isMcp = context.Items.ContainsKey(ChatContext.McpTransport);
        var to = new JsonArray();
        foreach (var name in names)
        {
            var tool = registry.GetTool(name!, req);
            if (tool == null)
            {
                to.Add(new JsonObject { ["name"] = name, ["error"] = "Not found or not available to you" });
                continue;
            }

            var describe = CreateToolSchema(tool);
            // For MCP callers, overlay the root schema description with [Mcp(Description=..)]
            // when set — lets hosts give MCP agents imperative wording without touching the
            // regular [Description] read by OpenAPI generators and admin UIs.
            if (isMcp && !string.IsNullOrEmpty(tool.McpDescription))
                describe["description"] = tool.McpDescription;
            var toolAnnotation = describe["tool"]!.AsObject();
            if (!string.IsNullOrEmpty(tool.WhenToUse))
                toolAnnotation["whenToUse"] = tool.WhenToUse;
            if (tool.Examples.Count > 0)
                toolAnnotation["examples"] = new JsonArray(tool.Examples.Select(x => (JsonNode)x).ToArray());
            to.Add(describe);
        }
        return Task.FromResult<object?>(new JsonObject
        {
            ["status"] = "success",
            ["count"] = to.Count,
            ["apis"] = to,
            ["next"] = "Call api_call with the selected API name and arguments matching inputSchema",
        });
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

        var dtoArgs = args["args"] as JsonObject;
        var argsJson = dtoArgs?.ToJsonString(ChatJson.Options);
        Log.LogInformation("api_call {Api} as {User}", tool.RequestType, context.User);

        var response = await registry.ExecuteAsync(tool, argsJson, req).ConfigAwait();
        return FormatResult(tool, proposedArgs: dtoArgs, response);
    }

    Task<ChatToolApprovalRequest?> ApprovalAsync(JsonObject args, ChatContext context)
    {
        if (RequestOf(context) is not { } req)
            return Task.FromResult<ChatToolApprovalRequest?>(null);

        var name = args.GetString("name");
        var tool = !string.IsNullOrEmpty(name) ? registry.GetTool(name, req) : null;
        if (tool == null || (!tool.RequiresApproval && tool.Safety == ToolSafety.ReadOnly))
            return Task.FromResult<ChatToolApprovalRequest?>(null);

        var proposedArgs = args["args"] is JsonObject dtoArgs ? dtoArgs.Clone() : new JsonObject();
        var isMcp = context.Items.ContainsKey(ChatContext.McpTransport);
        return Task.FromResult<ChatToolApprovalRequest?>(new ChatToolApprovalRequest
        {
            Title = tool.Name,
            Description = isMcp ? tool.McpSummary : tool.Summary,
            Safety = tool.Safety,
            Schema = CreateToolSchema(tool),
            Arguments = proposedArgs,
            Metadata = new JsonObject
            {
                ["apiName"] = tool.Name,
                ["requestType"] = tool.RequestType,
                ["method"] = tool.Method,
                ["route"] = tool.Route,
            },
        });
    }

    internal JsonObject FormatResult(ApiTool tool, JsonObject? proposedArgs, object? response)
    {
        var json = ChatJson.Serialize(response);
        var truncated = json.Length > MaxResultLength;
        return new JsonObject
        {
            ["status"] = "success",
            ["api"] = tool.Name,
            // Always emit an object (never null) — CallOutputSchema declares `request` as
            // `{"type":"object"}`, and strict MCP clients (e.g. ZCode) fail schema validation
            // with "data/request must be object" when a caller omits `args` (e.g. IGet DTOs
            // like GetCoffeeShopMenu that take no parameters).
            ["request"] = proposedArgs?.Clone() ?? new JsonObject(),
            ["response"] = truncated
                ? json[..MaxResultLength] + $"\n...[truncated at {MaxResultLength} chars, narrow the query]"
                : ChatJson.Parse(json),
            ["truncated"] = truncated,
            ["next"] = tool.FollowUps.Count > 0
                ? new JsonArray(tool.FollowUps.Select(x => (JsonNode)x).ToArray())
                : null,
        };
    }

    internal ApiTool? GetTool(string name, IRequest req) => registry.GetTool(name, req);

    internal Task<object?> ExecuteAsync(ApiTool tool, JsonObject args, IRequest req) =>
        registry.ExecuteAsync(tool, args.ToJsonString(ChatJson.Options), req);

    static JsonObject CreateToolSchema(ApiTool tool)
    {
        var schema = (ChatJson.ToNode(tool.InputSchema) as JsonObject) ?? new JsonObject();
        schema["inputSchema"] = schema.Clone();
        if (tool.OutputSchema != null)
            schema["outputSchema"] = ChatJson.ToNode(tool.OutputSchema);
        if (tool.Prerequisites.Count > 0)
            schema["prerequisites"] = new JsonArray(tool.Prerequisites.Select(x => (JsonNode)x).ToArray());
        if (tool.Preview != null)
            schema["preview"] = tool.Preview;
        if (tool.FollowUps.Count > 0)
            schema["followUps"] = new JsonArray(tool.FollowUps.Select(x => (JsonNode)x).ToArray());
        schema["tool"] = new JsonObject
        {
            // This can differ from `request` when [Tool(Name)] defines an alias. api_call uses it.
            ["name"] = tool.Name,
            ["safety"] = tool.Safety.ToString().ToLower(),
            ["requiresApproval"] = tool.RequiresApproval
                || tool.Safety is ToolSafety.Write or ToolSafety.Destructive,
        };
        return schema;
    }

    static JsonObject SearchOutputSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["status"] = new JsonObject { ["type"] = "string" },
            ["count"] = new JsonObject { ["type"] = "integer" },
            ["apis"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "object" } },
            ["next"] = new JsonObject { ["type"] = "string" },
        },
        ["required"] = new JsonArray("status", "count", "apis"),
    };

    static JsonObject DescribeOutputSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["status"] = new JsonObject { ["type"] = "string" },
            ["count"] = new JsonObject { ["type"] = "integer" },
            ["apis"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "object" } },
            ["next"] = new JsonObject { ["type"] = "string" },
        },
        ["required"] = new JsonArray("status", "count", "apis"),
    };

    static JsonObject CallOutputSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["status"] = new JsonObject { ["type"] = "string" },
            ["api"] = new JsonObject { ["type"] = "string" },
            ["request"] = new JsonObject { ["type"] = "object" },
            ["response"] = new JsonObject(),
            ["truncated"] = new JsonObject { ["type"] = "boolean" },
            ["confirmationToken"] = new JsonObject { ["type"] = "string" },
            ["expiresInSeconds"] = new JsonObject { ["type"] = "integer" },
            ["summary"] = new JsonObject { ["type"] = "string" },
            ["instruction"] = new JsonObject { ["type"] = "string" },
        },
        ["required"] = new JsonArray("status", "api"),
    };

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
