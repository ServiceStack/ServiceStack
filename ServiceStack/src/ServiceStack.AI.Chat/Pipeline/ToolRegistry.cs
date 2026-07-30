using System.Text.Json.Nodes;

namespace ServiceStack.AI;

public delegate Task<object?> ChatToolHandler(JsonObject args, ChatContext context);

/// <summary>An LLM tool: OpenAI function-call JSON schema + its executable handler</summary>
public class ChatTool
{
    /// <summary>{"type":"function","function":{"name":...,"description":...,"parameters":{...}}}</summary>
    public required JsonObject Definition { get; init; }
    public required ChatToolHandler Handler { get; init; }
    public string? Group { get; init; }

    /// <summary>
    /// How much damage a call can do, when the tool says. Kept off the wire definition — providers
    /// reject unknown fields inside "function" — and surfaced to Agents that model it, e.g. as MCP
    /// tool annotations.
    /// </summary>
    public ToolSafety Safety { get; init; }

    public string Name => Definition.GetObject("function").GetString("name")
        ?? throw new ArgumentException("Tool definition missing function.name");
}

/// <summary>
/// Registry of tools available to the chat tool-execution loop (port of AppExtensions.tools/tool_groups).
/// </summary>
public class ToolRegistry
{
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
}
