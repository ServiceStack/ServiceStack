using System.Text.Json.Nodes;

namespace ServiceStack.AI;

public delegate Task<object?> ChatToolHandler(JsonObject args, ChatContext context);

/// <summary>An LLM tool: OpenAI function-call JSON schema + its executable handler</summary>
public class ChatTool
{
    /// <summary>{"type":"function","function":{"name":...,"description":...,"parameters":{...}}}</summary>
    public required JsonObject Definition { get; init; }
    public required ChatToolHandler Handler { get; init; }
    /// <summary>Optional JSON Schema for structured MCP tool results.</summary>
    public JsonObject? OutputSchema { get; init; }
    /// <summary>Optional preflight for model-generated calls that may need human approval.</summary>
    public ChatToolApprovalHandler? ApprovalHandler { get; init; }
    public string? Group { get; init; }
    /// <summary>Contextual tools preserve remote arguments and are never exported by the inbound MCP server.</summary>
    public string? Source { get; init; }

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
}

/// <summary>Resolves authorized tools without modifying the global local-tool registry.</summary>
public interface IChatToolProvider
{
    Task<IReadOnlyList<ChatTool>> ResolveAsync(ChatContext context, string selector);
}

/// <summary>A request-scoped catalog. Definitions are copied so provider adaptation cannot mutate it.</summary>
public sealed class ResolvedChatTools
{
    readonly System.Collections.Frozen.FrozenDictionary<string, ChatTool> tools;
    public ResolvedChatTools(IEnumerable<ChatTool> tools)
    {
        this.tools = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(tools.Select(x => new ChatTool {
            Definition = (JsonObject)x.Definition.DeepClone(), Handler = x.Handler,
            OutputSchema = (JsonObject?)x.OutputSchema?.DeepClone(), ApprovalHandler = x.ApprovalHandler,
            Group = x.Group, Source = x.Source, Safety = x.Safety,
        }), x => x.Name, StringComparer.Ordinal);
    }
    public IEnumerable<ChatTool> Tools => tools.Values;
    public ChatTool? GetTool(string name) => tools.GetValueOrDefault(name);
}
