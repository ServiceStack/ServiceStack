#nullable enable

using System;

namespace ServiceStack;

/// <summary>
/// MCP-specific metadata for a Request DTO exposed as a tool. Lets you tell MCP clients
/// something you don't want in the regular <c>[Description]</c> (which is also read by
/// OpenAPI generators, admin UIs, and non-MCP consumers).
/// <para>
/// When present, the MCP endpoint prefers <see cref="Description"/> over
/// <c>[System.ComponentModel.Description]</c> in the responses MCP clients see
/// (e.g. <c>api_describe</c>, <c>requires_confirmation</c> summaries).
/// The regular <c>[Description]</c> is still used everywhere else and as the fallback.
/// </para>
/// </summary>
/// <example>
/// <code>
/// [Description("Submits a coffee shop order")] // clean, for OpenAPI / UIs
/// [Mcp(Description = "Submits a coffee shop order. IMPORTANT: You MUST first "
///     + "call PreviewCoffeeShopOrder and obtain the customer's explicit "
///     + "natural-language confirmation of the itemized total before calling this.")]
/// [Tool("the user wants to place an order", Safety = ToolSafety.Write, RequiresApproval = true)]
/// public class CreateCoffeeShopOrder : IPost, IReturn&lt;CreateCoffeeShopOrderResponse&gt; { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class McpAttribute : AttributeBase
{
    /// <summary>
    /// MCP-visible description of this API. Overrides the DTO's <c>[Description]</c> in
    /// MCP responses (<c>api_describe</c>, <c>requires_confirmation</c> summaries) so you
    /// can give MCP-native agents imperative wording (MUST / WAIT / …) without polluting
    /// the description read by OpenAPI generators and admin UIs.
    /// </summary>
    public string? Description { get; set; }

    public McpAttribute() { }

    /// <summary>Set the MCP-visible <see cref="Description"/>.</summary>
    public McpAttribute(string description) => Description = description;
}
