using System.Text.Json.Nodes;

namespace ServiceStack.AI;

public delegate Task<object?> ChatToolHandler(JsonObject args, ChatContext context);

/// <summary>An LLM tool: OpenAI function-call JSON schema + its executable handler</summary>
public class ChatTool
{
    /// <summary>{"type":"function","function":{"name":...,"description":...,"parameters":{...}}}</summary>
    public required JsonObject Definition { get; init; }
    public required ChatToolHandler Handler { get; init; }
    /// <summary>Optional preflight for model-generated calls that may need human approval.</summary>
    public ChatToolApprovalHandler? ApprovalHandler { get; init; }
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
}
