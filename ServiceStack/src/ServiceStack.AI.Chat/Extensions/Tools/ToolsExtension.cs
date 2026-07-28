using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Tool management (port of llms-py's "tools" extension): lists registered tool groups and
/// definitions for the UI's tools panel, executes tools directly, and serves the server-tools
/// config (user override &gt; default user &gt; bundled chat/ext/tools/server-tools.json).
/// </summary>
public class ToolsExtension : IChatExtension
{
    public string Name => ChatExtension.Tools;

    public void Install(ExtensionContext ctx)
    {
        ctx.AddGet("", _ =>
        {
            var groups = new JsonObject();
            foreach (var entry in ctx.Feature.Tools.Groups)
            {
                groups[entry.Key] = new JsonArray(entry.Value.Select(x => (JsonNode)x).ToArray());
            }
            var definitions = new JsonArray();
            foreach (var tool in ctx.Feature.Tools.Tools.Values)
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

            var toolDef = ctx.GetToolDefinition(name)
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

            var context = new ChatContext { User = req.UserName };
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
                    ctx.Log.LogError(e, "Error reading tools from {Path}", path);
                }
            }

            var bundled = HostContext.VirtualFileSources.GetFile("chat/ext/tools/server-tools.json");
            return Task.FromResult<object?>(bundled != null
                ? new ChatResult { Text = bundled.ReadAllText(), ContentType = MimeTypes.Json }
                : new JsonArray());
        });
    }
}
