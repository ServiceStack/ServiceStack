#nullable enable
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.Extensions.Tests;

public class AiChatRouteTests
{
    static RouteRegistry CreateRegistry()
    {
        var routes = new RouteRegistry();
        // registration order mirrors llms-py: specific routes before wildcard statics
        routes.AddGet("/models", _ => Task.FromResult<object?>("models"));
        routes.AddPost("/providers/{provider}", _ => Task.FromResult<object?>("provider"));
        routes.AddGet("/~cache/{tail:.*}", _ => Task.FromResult<object?>("cache"));
        routes.AddGet("/ui/{path:.*}", _ => Task.FromResult<object?>("ui"));
        routes.AddGet("/ext/app/threads", _ => Task.FromResult<object?>("threads"));
        routes.AddGet("/ext/app/threads/sync", _ => Task.FromResult<object?>("sync"));
        routes.AddGet("/ext/app/threads/{id}", _ => Task.FromResult<object?>("thread"));
        routes.AddGet("/ext/app/threads/{id}/updates", _ => Task.FromResult<object?>("updates"));
        routes.AddPost("/ext/app/threads/{id}/chat", _ => Task.FromResult<object?>("chat"));
        routes.AddGet("/ext/app/{path:.*}", _ => Task.FromResult<object?>("static"));
        return routes;
    }

    [Test]
    public void Matches_literal_routes()
    {
        var routes = CreateRegistry();
        var match = routes.Match("GET", "/models");
        Assert.That(match, Is.Not.Null);
        Assert.That(match!.Value.Route.Template, Is.EqualTo("/models"));

        Assert.That(routes.Match("POST", "/models"), Is.Null);
        Assert.That(routes.Match("GET", "/unknown"), Is.Null);
    }

    [Test]
    public void Matches_path_params()
    {
        var routes = CreateRegistry();
        var match = routes.Match("POST", "/providers/openrouter");
        Assert.That(match, Is.Not.Null);
        Assert.That(match!.Value.Params["provider"], Is.EqualTo("openrouter"));
    }

    [Test]
    public void Registration_order_wins_for_overlapping_routes()
    {
        var routes = CreateRegistry();
        // /threads/sync registered before /threads/{id} so literal match wins
        Assert.That(routes.Match("GET", "/ext/app/threads/sync")!.Value.Route.Template,
            Is.EqualTo("/ext/app/threads/sync"));
        Assert.That(routes.Match("GET", "/ext/app/threads/123")!.Value.Params["id"],
            Is.EqualTo("123"));
        Assert.That(routes.Match("GET", "/ext/app/threads/123/updates")!.Value.Route.Template,
            Is.EqualTo("/ext/app/threads/{id}/updates"));
        Assert.That(routes.Match("POST", "/ext/app/threads/123/chat")!.Value.Route.Template,
            Is.EqualTo("/ext/app/threads/{id}/chat"));
    }

    [Test]
    public void Specific_routes_match_before_wildcard_statics()
    {
        var routes = CreateRegistry();
        Assert.That(routes.Match("GET", "/ext/app/threads")!.Value.Route.Template,
            Is.EqualTo("/ext/app/threads"));
        // non-route paths under the extension fall through to its static wildcard
        var match = routes.Match("GET", "/ext/app/index.mjs");
        Assert.That(match!.Value.Route.Template, Is.EqualTo("/ext/app/{path:.*}"));
        Assert.That(match.Value.Params["path"], Is.EqualTo("index.mjs"));
    }

    [Test]
    public void Wildcards_capture_nested_paths()
    {
        var routes = CreateRegistry();
        var match = routes.Match("GET", "/~cache/ab/abc123.png");
        Assert.That(match!.Value.Params["tail"], Is.EqualTo("ab/abc123.png"));

        match = routes.Match("GET", "/ui/modules/chat/index.mjs");
        Assert.That(match!.Value.Params["path"], Is.EqualTo("modules/chat/index.mjs"));

        // wildcard can capture empty rest
        match = routes.Match("GET", "/ui/");
        Assert.That(match!.Value.Params["path"], Is.EqualTo(""));
    }

    [Test]
    public void Url_decodes_path_params()
    {
        var routes = CreateRegistry();
        var match = routes.Match("POST", "/providers/my%20provider");
        Assert.That(match!.Value.Params["provider"], Is.EqualTo("my provider"));
    }
}
