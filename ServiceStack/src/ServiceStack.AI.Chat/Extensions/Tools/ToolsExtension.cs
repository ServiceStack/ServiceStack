using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Tool management (port of llms-py's "tools" extension): lists registered tool groups and
/// definitions for the UI's tools panel, executes tools directly, and serves the server-tools
/// config (user override &gt; default user &gt; bundled chat/ext/tools/server-tools.json).
/// </summary>
public class ToolsExtension() : ChatExtension("tools")
{
    /// <summary>Allow LLM tools to execute code on the server (core_tools run_*, computer run_bash). OFF by default.</summary>
    public bool EnableCodeExecution { get; set; }
    /// <summary>Allow LLM filesystem tools (computer extension). OFF by default.</summary>
    public bool EnableFilesystemTools { get; set; }
    /// <summary>Directories LLM filesystem/code tools may access</summary>
    public List<string> AllowedDirectories { get; set; } = [];
    public TimeSpan ToolTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Let LLM tools discover and call this App's own ServiceStack APIs. OFF by default.</summary>
    public bool EnableApiTools { get; set; } = true;

    /// <summary>
    /// Tools registry
    /// </summary>
    public Dictionary<string, ChatTool> Tools { get; } = [];
    public Dictionary<string, List<string>> Groups { get; } = [];

    public void Register(ChatTool tool)
    {
        var name = tool.Name;
        Tools[name] = tool;
        if (tool.Group != null)
        {
            var group = Groups.GetOrAdd(tool.Group, _ => []);
            group.AddIfNotExists(name);
        }
    }

    public ChatTool? GetTool(string name) => Tools.GetValueOrDefault(name);

    public JsonObject? GetToolDefinition(string name) => GetTool(name)?.Definition;

    /// <summary>
    /// Resolve a tool selector ("all" | "none" | csv of tool or group names) to tool definitions
    /// (port of AppExtensions.create_chat_with_tools selector logic).
    /// </summary>
    public List<ChatTool> SelectTools(string? use)
    {
        use ??= "all";
        if (use == "none")
            return [];
        if (use == "all")
            return Tools.Values.ToList();

        var ret = new List<ChatTool>();
        foreach (var name in use.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0))
        {
            if (Groups.TryGetValue(name, out var groupTools))
            {
                foreach (var toolName in groupTools)
                {
                    if (Tools.TryGetValue(toolName, out var groupTool) && !ret.Contains(groupTool))
                        ret.Add(groupTool);
                }
            }
            else if (Tools.TryGetValue(name, out var tool) && !ret.Contains(tool))
            {
                ret.Add(tool);
            }
        }
        return ret;
    }

    public override void Install(ExtensionContext ctx)
    {
        if (AllowedDirectories.Count == 0)
        {
            AllowedDirectories.Add(ctx.AppData.BasePath.CombineWith("workspace").AssertDir());
        }
        
        ctx.AddGet("", _ =>
        {
            var groups = new JsonObject();
            foreach (var entry in Groups)
            {
                groups[entry.Key] = new JsonArray(entry.Value.Select(x => (JsonNode)x).ToArray());
            }
            var definitions = new JsonArray();
            foreach (var tool in Tools.Values)
            {
                definitions.Add(tool.Definition.Clone());
            }
            return Task.FromResult<object?>(new JsonObject
            {
                ["groups"] = groups,
                ["definitions"] = definitions,
            });
        });

        ctx.AddPost("exec/{name}", async req =>
        {
            var name = req.GetPathParam("name");
            var args = await req.GetJsonBodyAsync().ConfigAwait();

            var toolDef = Ctx.GetToolDefinition(name)
                ?? throw new Exception($"Tool '{name}' not found");
            var type = toolDef.GetString("type");
            if (type != "function")
                throw new Exception($"Tool '{name}' of type '{type}' is not supported");

            // only pass args declared in the tool's schema
            var functionArgs = new JsonObject();
            if (toolDef.GetObject("function").GetObject("parameters").GetObject("properties") is { } properties)
            {
                foreach (var entry in args)
                {
                    if (properties.ContainsKey(entry.Key))
                        functionArgs[entry.Key] = entry.Value?.DeepClone();
                }
            }

            var context = new ChatContext { User = req.UserName, Request = req.Request };
            var (text, resources) = await ctx.Feature.ExecToolAsync(name, functionArgs, context).ConfigAwait();

            var results = new JsonArray();
            if (!string.IsNullOrEmpty(text))
            {
                results.Add(new JsonObject { ["type"] = "text", ["text"] = text });
            }
            foreach (var resource in resources)
            {
                results.Add(resource.Clone());
            }
            return results;
        });

        ctx.AddGet("server", req =>
        {
            var user = req.UserName;
            var paths = new List<string>();
            if (user != null)
                paths.Add(Path.Combine(ctx.GetUserPath(user), "server-tools.json"));
            paths.Add(Path.Combine(ctx.GetUserPath(), "server-tools.json"));

            foreach (var path in paths)
            {
                if (!File.Exists(path))
                    continue;
                try
                {
                    return Task.FromResult<object?>(new ChatResult
                    {
                        Text = File.ReadAllText(path),
                        ContentType = MimeTypes.Json,
                    });
                }
                catch (Exception e)
                {
                    Log.LogError(e, "Error reading tools from {Path}", path);
                }
            }

            var bundled = HostContext.VirtualFileSources.GetFile("chat/ext/tools/server-tools.json");
            return Task.FromResult<object?>(bundled != null
                ? new ChatResult { Text = bundled.ReadAllText(), ContentType = MimeTypes.Json }
                : new JsonArray());
        });
    }
}
