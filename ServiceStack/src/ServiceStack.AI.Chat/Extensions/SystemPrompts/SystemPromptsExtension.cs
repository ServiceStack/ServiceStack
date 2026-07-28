using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>
/// System prompt library (port of extensions/system_prompts): GET /ext/system_prompts/prompts.json
/// resolves user prompts &gt; default-user prompts &gt; bundled chat/ext/system_prompts/prompts.json.
/// </summary>
public class SystemPromptsExtension : IChatExtension
{
    public string Name => ChatExtension.SystemPrompts;

    public void Install(ExtensionContext ctx)
    {
        ctx.AddGet("prompts.json", ChatRequestContext =>
        {
            var candidatePaths = new List<string>();
            var user = ctx.GetUserName(ChatRequestContext.Request);
            if (user != null)
            {
                candidatePaths.Add(Path.Combine(ctx.GetUserPath(user), "system_prompts", "prompts.json"));
            }
            candidatePaths.Add(Path.Combine(ctx.GetUserPath(), "system_prompts", "prompts.json"));

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    return Task.FromResult<object?>(new ChatResult
                    {
                        Text = File.ReadAllText(path),
                        ContentType = MimeTypes.Json,
                    });
                }
            }

            // bundled prompts synced from llms-py
            var bundled = HostContext.VirtualFileSources.GetFile("chat/ext/system_prompts/prompts.json");
            if (bundled != null)
            {
                return Task.FromResult<object?>(new ChatResult
                {
                    Text = bundled.ReadAllText(),
                    ContentType = MimeTypes.Json,
                });
            }

            return Task.FromResult<object?>(new JsonArray
            {
                new JsonObject { ["name"] = "Helpful Assistant", ["prompt"] = "You are a helpful assistant." }
            });
        });
    }
}
