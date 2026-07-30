#nullable enable

using System;

namespace ServiceStack;

/// <summary>
/// How much damage a tool call can do, which determines whether an AI Agent may call it unattended.
/// </summary>
public enum ToolSafety
{
    /// <summary>
    /// Infer from the API's HTTP Verb: GET/HEAD is <see cref="ReadOnly"/>, DELETE is
    /// <see cref="Destructive"/>, everything else is <see cref="Write"/>. The default.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Only reads data, safe to call unattended and safe to retry.
    /// </summary>
    ReadOnly = 1,

    /// <summary>
    /// Creates or updates data. Recoverable, but retrying may duplicate the change.
    /// </summary>
    Write = 2,

    /// <summary>
    /// Deletes data or triggers a real-world side effect (sends an email, charges a card).
    /// Hosts should require user approval before executing these.
    /// </summary>
    Destructive = 3,
}

/// <summary>
/// Opt an API in to being called by AI Agents as a tool, i.e. an LLM can discover this API,
/// read its schema and invoke it with a JSON payload.
/// <para>
/// Existing API metadata is used as-is and should not be repeated here: <c>[Description]</c> and
/// <c>[Notes]</c> document what the API does, <c>[Tag]</c> groups it, and property-level
/// <c>[Description]</c>/<c>[ApiMember]</c>/<c>[ApiAllowableValues]</c> become the JSON Schema an
/// Agent reads before calling it. This attribute adds only what an Agent needs beyond that.
/// </para>
/// </summary>
/// <example>
/// <code>
/// [Tag("Northwind")]
/// [Description("Search Customers by company, contact or country")]
/// [Tool("the user asks who a customer is, where they're based or how to contact them",
///     Keywords = ["client", "account", "buyer"],
///     Examples = ["""{"countryStartsWith":"UK","take":10}"""],
///     Fields = "id,companyName,country", Take = 25)]
/// public class QueryCustomers : QueryDb&lt;Customer&gt; { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ToolAttribute : AttributeBase
{
    /// <summary>
    /// The name the AI Agent calls this tool by. Defaults to the Request DTO name.
    /// <para>
    /// Set this when you want a stable tool name that survives renaming the DTO, or to match a
    /// naming convention (tools are conventionally snake_case, e.g. "find_customers").
    /// Must be unique across all exposed APIs.
    /// </para>
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// When an Agent should reach for this API, phrased as the situation that calls for it,
    /// e.g. "the user asks about a customer's orders, spend or contact details".
    /// <para>
    /// The single most valuable thing you can add. <c>[Description]</c> tells a developer reading
    /// your API docs what this API <i>does</i>; this tells an Agent <i>when to pick it</i> out of
    /// everything else available, which is a different sentence. Omitting it doesn't disable the
    /// tool, it just leaves selection to the API name and description alone.
    /// </para>
    /// </summary>
    public string? WhenToUse { get; set; }

    /// <summary>
    /// Extra search terms an Agent might use to look for this API, e.g. ["client","account"].
    /// <para>
    /// APIs are discovered by searching a compact index of names, routes, tags and descriptions,
    /// so add the words a user would say that don't already appear in any of those. A
    /// <c>QueryCustomers</c> API is not found by searching "who bought from us".
    /// </para>
    /// </summary>
    public string[]? Keywords { get; set; }

    /// <summary>
    /// Example JSON request payloads returned alongside this API's schema, e.g.
    /// <c>["""{"nameStartsWith":"A","orderBy":"-id","take":10}"""]</c>.
    /// <para>
    /// The highest accuracy-per-token you can buy for APIs whose usable inputs aren't obvious from
    /// their type alone — AutoQuery's implicit conventions (<c>%StartsWith</c>, <c>%Between</c>,
    /// <c>orderBy</c>, <c>fields</c>, <c>take</c>) don't appear in the generated schema at all.
    /// One realistic example teaches them all.
    /// </para>
    /// </summary>
    public string[]? Examples { get; set; }

    /// <summary>
    /// How much damage this tool call can do, used to decide whether an Agent may call it
    /// unattended. Defaults to <see cref="ToolSafety.Auto"/>, inferred from the API's HTTP Verb.
    /// <para>
    /// Set it explicitly when the verb lies about the consequences, e.g. a POST that only runs a
    /// report is <see cref="ToolSafety.ReadOnly"/>; a POST that emails every customer is
    /// <see cref="ToolSafety.Destructive"/>.
    /// </para>
    /// </summary>
    public ToolSafety Safety { get; set; }

    /// <summary>
    /// Require the user to approve each call before it executes, regardless of <see cref="Safety"/>.
    /// <para>
    /// Use for APIs that are cheap to call but expensive to get wrong, where you always want a
    /// human in the loop even though an Agent could technically call them freely.
    /// </para>
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Comma-delimited fields the response is reduced to when the Agent doesn't ask for specific
    /// fields, e.g. "id,companyName,country".
    /// <para>
    /// The main defence against a single call flooding the Agent's context: a table with 90 columns
    /// costs more to return once than the entire tool index costs to keep loaded. Only limits the
    /// default, the Agent can still request other fields.
    /// </para>
    /// </summary>
    public string? Fields { get; set; }

    /// <summary>
    /// Maximum rows returned when the Agent doesn't specify its own limit. 0 (default) uses the
    /// host's configured default.
    /// <para>
    /// Applies to APIs returning a result set, e.g. AutoQuery. Prefer a small number: an Agent that
    /// needs more can page or filter, whereas one that receives 10,000 rows has already spent the
    /// context it needed to use them.
    /// </para>
    /// </summary>
    public int Take { get; set; }

    /// <summary>
    /// The tool group this API is listed under, letting users enable or disable related tools
    /// together. Defaults to the API's first <c>[Tag]</c>.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Hide this API from Agents even though it would otherwise be included, e.g. because the host
    /// exposes its whole <c>[Tag]</c> in bulk. Takes precedence over every other opt-in.
    /// </summary>
    public bool Exclude { get; set; }

    public ToolAttribute() { }

    /// <summary>
    /// Opt this API in to being called by AI Agents, setting <see cref="WhenToUse"/>.
    /// </summary>
    /// <param name="whenToUse">
    /// When an Agent should reach for this API, e.g. "the user asks how much a customer has spent"
    /// </param>
    public ToolAttribute(string whenToUse) => WhenToUse = whenToUse;
}
