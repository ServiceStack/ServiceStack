using System.Text.Json.Nodes;

namespace ServiceStack.AI;

public delegate Task<object?> ChatRouteHandler(ChatRequestContext ctx);

/// <summary>
/// Ordered route table mirroring aiohttp's router semantics used by llms-py:
/// routes match in registration order, templates support "{name}" segments and
/// trailing "{name:.*}" wildcards (e.g. "/ext/app/threads/{id}", "/~cache/{tail:.*}").
/// </summary>
public class RouteRegistry
{
    public record Route(string Method, string Template, ChatRouteHandler Handler, string[] Segments, string? WildcardParam);

    public List<Route> Routes { get; } = [];

    public void AddGet(string path, ChatRouteHandler handler) => Add(HttpMethods.Get, path, handler);
    public void AddPost(string path, ChatRouteHandler handler) => Add(HttpMethods.Post, path, handler);
    public void AddPut(string path, ChatRouteHandler handler) => Add(HttpMethods.Put, path, handler);
    public void AddDelete(string path, ChatRouteHandler handler) => Add(HttpMethods.Delete, path, handler);
    public void AddPatch(string path, ChatRouteHandler handler) => Add(HttpMethods.Patch, path, handler);

    public void Add(string method, string template, ChatRouteHandler handler)
    {
        if (!template.StartsWith('/'))
            template = "/" + template;

        string? wildcardParam = null;
        var segments = template.Length == 1 ? [] : template[1..].Split('/');
        if (segments.Length > 0)
        {
            var last = segments[^1];
            if (last.StartsWith('{') && last.EndsWith(":.*}"))
                wildcardParam = last[1..^4];
        }
        Routes.Add(new Route(method.ToUpper(), template, handler, segments, wildcardParam));
    }

    /// <summary>Find the first registered route matching method + path. Returns null if no match.</summary>
    public (Route Route, Dictionary<string, string> Params)? Match(string method, string path)
    {
        if (string.IsNullOrEmpty(path)) path = "/";
        var pathSegments = path.Length == 1 ? [] : path[1..].Split('/');

        foreach (var route in Routes)
        {
            if (route.Method != method)
                continue;
            var match = MatchRoute(route, pathSegments);
            if (match != null)
                return (route, match);
        }
        return null;
    }

    static Dictionary<string, string>? MatchRoute(Route route, string[] pathSegments)
    {
        var template = route.Segments;
        if (route.WildcardParam != null)
        {
            // all segments before the wildcard must match; wildcard captures the rest (can be empty)
            if (pathSegments.Length < template.Length - 1)
                return null;
        }
        else if (pathSegments.Length != template.Length)
        {
            return null;
        }

        Dictionary<string, string>? args = null;
        var fixedCount = route.WildcardParam != null ? template.Length - 1 : template.Length;
        for (var i = 0; i < fixedCount; i++)
        {
            var t = template[i];
            if (t.StartsWith('{') && t.EndsWith('}'))
            {
                var value = pathSegments[i].UrlDecode();
                if (string.IsNullOrEmpty(value))
                    return null;
                args ??= new();
                args[t[1..^1]] = value;
            }
            else if (!string.Equals(t, pathSegments[i], StringComparison.Ordinal))
            {
                return null;
            }
        }

        args ??= new();
        if (route.WildcardParam != null)
        {
            var rest = pathSegments.Length > fixedCount
                ? string.Join('/', pathSegments[fixedCount..])
                : "";
            args[route.WildcardParam] = rest.Length > 0 ? rest.UrlDecode() : "";
        }
        return args;
    }
}
