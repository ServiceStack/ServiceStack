#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

/// <summary>
/// Which of an App's APIs are exposed to AI Agents. APIs annotated with [Tool] are always
/// included; these let a host expose APIs in bulk or override that opt-in.
/// </summary>
public class ApiToolsConfig
{
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
}

/// <summary>
/// An API exposed to AI Agents: its resolved [Tool] metadata, the access it requires, and the
/// JSON Schema of its Request DTO. Transport-neutral — the same instance backs the Chat UI's
/// api_search/api_describe/api_call tools and can back an MCP tools/list + tools/call endpoint.
/// </summary>
public class ApiTool
{
    /// <summary>The name Agents call this tool by ([Tool(Name)], defaults to the Request DTO name)</summary>
    public string Name { get; set; } = null!;
    /// <summary>Request DTO name, used to resolve the type to execute</summary>
    public string RequestType { get; set; } = null!;
    public Type Type { get; set; } = null!;

    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? WhenToUse { get; set; }
    public List<string> Keywords { get; set; } = [];
    public List<string> Examples { get; set; } = [];

    public ToolSafety Safety { get; set; }
    public bool RequiresApproval { get; set; }
    public string? Fields { get; set; }
    public int Take { get; set; }
    public string? Group { get; set; }
    public List<string> Tags { get; set; } = [];

    public string? Method { get; set; }
    public string? Route { get; set; }

    public bool RequiresAuth { get; set; }
    public bool RequiresApiKey { get; set; }
    public List<string> RequiredRoles { get; set; } = [];
    public List<string> RequiresAnyRole { get; set; } = [];
    public List<string> RequiredPermissions { get; set; } = [];
    public List<string> RequiresAnyPermission { get; set; } = [];

    /// <summary>JSON Schema of the Request DTO, generated on first use</summary>
    public Dictionary<string, object?> InputSchema { get; set; } = null!;

    /// <summary>What this API does, for a listing. Falls back to the API name when undocumented.</summary>
    public string Summary => !string.IsNullOrEmpty(Description) ? Description!
        : !string.IsNullOrEmpty(WhenToUse) ? $"Use when {WhenToUse}"
        : Name;

    /// <summary>One line per API for a search result listing — the Agent's index into the App's APIs</summary>
    public string ToSummaryLine() => Safety == ToolSafety.ReadOnly
        ? $"{Name} [{string.Join(",", Tags)}] {Summary}"
        : $"{Name} [{string.Join(",", Tags)}] ({Safety.ToString().ToLower()}) {Summary}";
}

/// <summary>
/// Discovers, describes and executes the App's own APIs on behalf of an AI Agent.
/// <para>
/// Opt-in only: an API is exposed when it's annotated with <see cref="ToolAttribute"/> or matches
/// <see cref="ApiToolsConfig"/>. The tool list is built once and cached; access is then checked per
/// request, because in-process execution does NOT run [Authenticate]/[RequiredRole] filter
/// attributes — this class enforces them itself, for discovery as well as execution.
/// </para>
/// </summary>
public class ApiToolRegistry(ApiToolsConfig config)
{
    public ApiToolsConfig Config => config;

    List<ApiTool>? allTools;
    readonly object buildLock = new();

    /// <summary>Every exposed API, regardless of who's asking. Built once from the App's metadata.</summary>
    public List<ApiTool> GetAll()
    {
        if (allTools != null)
            return allTools;
        lock (buildLock)
        {
            return allTools ??= Build();
        }
    }

    /// <summary>Only the APIs this request's user is allowed to call</summary>
    public List<ApiTool> GetTools(IRequest req) => GetAll().Where(x => CanAccess(x, req)).ToList();

    public ApiTool? GetTool(string name, IRequest req) => GetTools(req)
        .FirstOrDefault(x => x.Name == name || x.RequestType == name);

    /// <summary>Distinct [Tag]s across exposed APIs — a cheap map of the App to seed an Agent with</summary>
    public List<string> GetTags(IRequest req) => GetTools(req)
        .SelectMany(x => x.Tags).Distinct().OrderBy(x => x).ToList();

    List<ApiTool> Build()
    {
        var to = new List<ApiTool>();
        foreach (var op in HostContext.Metadata.Operations)
        {
            var type = op.RequestType;
            var attr = type.FirstAttribute<ToolAttribute>();
            if (!ShouldInclude(op, attr))
                continue;

            var safety = attr?.Safety ?? ToolSafety.Auto;
            if (safety == ToolSafety.Auto)
                safety = InferSafety(type, op.Method, op.Actions);

            to.Add(new ApiTool
            {
                Name = attr?.Name ?? op.Name,
                RequestType = op.Name,
                Type = type,
                Description = op.Description,
                Notes = op.Notes,
                WhenToUse = attr?.WhenToUse,
                Keywords = attr?.Keywords?.ToList() ?? [],
                Examples = attr?.Examples?.ToList() ?? [],
                Safety = safety,
                RequiresApproval = attr?.RequiresApproval ?? false,
                Fields = attr?.Fields,
                Take = attr?.Take ?? 0,
                Group = attr?.Group ?? op.Tags.FirstOrDefault(),
                Tags = op.Tags.ToList(),
                Method = op.Method,
                Route = op.Routes?.FirstOrDefault()?.Path,
                RequiresAuth = op.RequiresAuthentication,
                RequiresApiKey = op.RequiresApiKey,
                RequiredRoles = op.RequiredRoles.ToList(),
                RequiresAnyRole = op.RequiresAnyRole.ToList(),
                RequiredPermissions = op.RequiredPermissions.ToList(),
                RequiresAnyPermission = op.RequiresAnyPermission.ToList(),
                InputSchema = CreateInputSchema(type),
            });
        }
        return to;
    }

    /// <summary>
    /// The complete API Schema used by api_describe and schema-driven approval UIs. Provider
    /// function definitions use <see cref="ApiToolSchema"/>'s smaller, strict subset instead.
    /// </summary>
    public static Dictionary<string, object?> CreateInputSchema(Type type)
    {
#if NET8_0_OR_GREATER
        return MetadataSchemaGenerator.CreateSchema(type).ToJsonString()
            .FromJson<Dictionary<string, object?>>()!;
#else
        return ApiToolSchema.ToJsonSchema(type);
#endif
    }

    bool ShouldInclude(Operation op, ToolAttribute? attr)
    {
        if (attr?.Exclude == true)
            return false;
        if (config.ExcludeTypes.Contains(op.Name))
            return false;
        // never expose APIs hidden from metadata, they're not part of the App's public contract
        if (op.RequestType.HasAttributeOf<ExcludeMetadataAttribute>() || op.RestrictTo != null)
            return false;

        if (attr != null)
            return true;
        if (config.IncludeTypes.Contains(op.Name))
            return true;
        return config.IncludeTags.Count > 0 && op.Tags.Any(config.IncludeTags.Contains);
    }

    /// <summary>
    /// What a call costs when [Tool(Safety)] doesn't say, from the API's primary HTTP Method.
    /// </summary>
    static ToolSafety InferSafety(Type requestType, string? method, List<string>? actions)
    {
        // resolves IVerb, AutoQuery/CRUD interfaces and a single [Route] verb — an AutoQuery CRUD
        // service handles Any(), so its registered actions say nothing about what it does
        var verb = ServiceClientUtils.GetHttpMethod(requestType) ?? SingleVerb(method, actions);

        return verb?.ToUpper() switch
        {
            HttpMethods.Delete => ToolSafety.Destructive,
            HttpMethods.Get or HttpMethods.Head or HttpMethods.Options => ToolSafety.ReadOnly,
            // POST/PUT/PATCH, and anything still unknown: guessing "read-only" wrong is what lets
            // an Agent mutate data unattended, so an unrecognised API is assumed to write
            _ => ToolSafety.Write,
        };
    }

    /// <summary>The one verb this API is registered for, or null if it answers to several (or Any)</summary>
    static string? SingleVerb(string? method, List<string>? actions)
    {
        var verbs = (actions is { Count: > 0 } ? actions : method != null ? [method] : new List<string>())
            .Where(x => !x.EqualsIgnoreCase(ActionContext.AnyAction))
            .Select(x => x.ToUpper()).Distinct().ToList();
        return verbs.Count == 1 ? verbs[0] : null;
    }

    /// <summary>
    /// Whether this request's user may see and call this API. In-process execution bypasses the
    /// [Authenticate]/[RequiredRole] request filters, so this is the only thing enforcing them —
    /// it gates discovery too, so an Agent can't learn that APIs it can't call exist.
    /// </summary>
    public bool CanAccess(ApiTool tool, IRequest req)
    {
        if (!tool.RequiresAuth && tool.RequiredRoles.Count == 0 && tool.RequiresAnyRole.Count == 0
            && tool.RequiredPermissions.Count == 0 && tool.RequiresAnyPermission.Count == 0)
            return true;

        var session = req.GetSession();
        if (session?.IsAuthenticated != true)
            return false;

        var authRepo = HostContext.AppHost.GetAuthRepository(req);
        using (authRepo as IDisposable)
        {
            if (tool.RequiredRoles.Any(role => !session.HasRole(role, authRepo)))
                return false;
            if (tool.RequiresAnyRole.Count > 0 && !tool.RequiresAnyRole.Any(role => session.HasRole(role, authRepo)))
                return false;
            if (tool.RequiredPermissions.Any(perm => !session.HasPermission(perm, authRepo)))
                return false;
            if (tool.RequiresAnyPermission.Count > 0
                && !tool.RequiresAnyPermission.Any(perm => session.HasPermission(perm, authRepo)))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Find APIs matching a query, ranked name &gt; keywords &gt; tags &gt; description. Matching on a
    /// compact index instead of sending every API's schema is what keeps an App's whole surface
    /// available without keeping it in context.
    /// </summary>
    public List<ApiTool> Search(IRequest req, string? query, string? tag = null, int take = 20)
    {
        var tools = GetTools(req);
        if (!string.IsNullOrEmpty(tag))
            tools = tools.Where(x => x.Tags.Any(t => t.EqualsIgnoreCase(tag))).ToList();

        if (string.IsNullOrWhiteSpace(query))
            return tools.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Take(take).ToList();

        var terms = query!.Split([' ', ',', '|'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x => x.Length > 1).ToList();
        if (terms.Count == 0)
            terms.Add(query.Trim());

        return tools.Select(x => new { Tool = x, Score = Score(x, terms) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Tool.Name, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .Select(x => x.Tool)
            .ToList();
    }

    static int Score(ApiTool tool, List<string> terms)
    {
        var score = 0;
        foreach (var term in terms)
        {
            if (tool.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 10;
            if (tool.Keywords.Any(x => x.Contains(term, StringComparison.OrdinalIgnoreCase))) score += 6;
            if (tool.Tags.Any(x => x.Contains(term, StringComparison.OrdinalIgnoreCase))) score += 4;
            if (tool.WhenToUse?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) score += 3;
            if (tool.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) score += 2;
            if (tool.Route?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) score += 1;
        }
        return score;
    }

    /// <summary>
    /// Execute an API as this request's user. Access is asserted first: the in-process gateway
    /// runs gateway filters and validators but not the DTO's auth filter attributes.
    /// </summary>
    public async Task<object?> ExecuteAsync(ApiTool tool, string? argsJson, IRequest req)
    {
        if (!CanAccess(tool, req))
            throw HttpError.Forbidden($"'{tool.Name}' requires access this user doesn't have");

        var dto = string.IsNullOrWhiteSpace(argsJson)
            ? tool.Type.CreateInstance()
            : JsonSerializer.DeserializeFromString(argsJson, tool.Type);
        if (dto == null)
            throw new ArgumentException($"Could not populate '{tool.RequestType}' from arguments");

        ApplyDefaults(dto, tool);

        var gateway = HostContext.AppHost.GetServiceGateway(req);
        return await gateway.SendAsync<object>(dto).ConfigAwait();
    }

    /// <summary>
    /// Cap what a query returns. The largest context cost isn't the tool definitions, it's one
    /// unbounded query returning every column of every row.
    /// </summary>
    void ApplyDefaults(object dto, ApiTool tool)
    {
        if (dto is not QueryBase query)
            return;
        query.Take ??= tool.Take > 0 ? tool.Take : config.DefaultTake;
        if (query.Take > config.MaxTake)
            query.Take = config.MaxTake;
        if (string.IsNullOrEmpty(query.Fields))
            query.Fields = tool.Fields;
    }
}
