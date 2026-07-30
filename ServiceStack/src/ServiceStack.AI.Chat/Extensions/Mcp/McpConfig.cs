namespace ServiceStack.AI;

/// <summary>
/// Which Chat tools this App exposes to external AI Agents over MCP, and how the server
/// identifies itself to them.
/// <para>
/// Deny by default: the endpoint is only mounted when <see cref="ToolGroups"/> or
/// <see cref="Tools"/> names something. Handing an external Agent the same tools the Chat UI
/// runs is a much wider blast radius than running them yourself — expose the groups you mean,
/// not "all", unless the App only registers tools that are safe to hand out.
/// </para>
/// </summary>
public class McpConfig
{
    /// <summary>Tool groups to expose, e.g. "api_tools", "core_tools". Empty disables the endpoint.</summary>
    public List<string> ToolGroups { get; set; } = [];

    /// <summary>Individual tools to expose, in addition to whole <see cref="ToolGroups"/></summary>
    public List<string> Tools { get; set; } = [];

    /// <summary>Server name reported to MCP Clients in initialize</summary>
    public string ServerName { get; set; } = "servicestack-ai-chat";

    /// <summary>Server version reported to MCP Clients (defaults to the ServiceStack version)</summary>
    public string? ServerVersion { get; set; }

    /// <summary>Optional usage hint Clients can add to their system prompt</summary>
    public string? Instructions { get; set; }

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
}
