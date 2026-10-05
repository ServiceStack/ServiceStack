#nullable enable
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.AiTests;

public partial class AiChatMigrationOpenAiAuthTests
{
    async Task<JsonObject> StartAutomatic(Fixture f, string user = "alice")
    {
        f.Ext.Options.AutomaticCallback = true;
        var result = (JsonObject)(await f.Host.SendAsync("POST", f.Host.Feature.RoutePrefix + "/ext/openai_auth/connect", user, new JsonObject()))!;
        f.Authorization = Query(result.GetString("auth_url")!);
        return result;
    }
    [TestCase(""), TestCase("/chat")]
    public async Task Automatic_callback_listener_completes_origin_users_grant_and_uses_exact_selected_redirect(string prefix)
    {
        Assert.That(new OpenAiSubscriptionOptions().AutomaticCallback, Is.True);
        using var f = new Fixture(prefix);
        f.Ext.Options.RedirectUri = "http://127.0.0.1:0/auth/callback";
        var connect = await StartAutomatic(f);
        Assert.That(connect.GetBool("automatic_callback"), Is.True);
        Assert.That(connect.GetBool("manual_callback"), Is.False);
        Assert.That(new Uri(connect.GetString("redirect_uri")!).Port, Is.GreaterThan(0));
        using var http = new HttpClient();
        using var response = await http.GetAsync(f.Callback());
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var html = await response.Content.ReadAsStringAsync();
        Assert.That(html, Does.Contain("Connected to ChatGPT").And.Contain("window.close()").And.Not.Contain("alice-access").And.Not.Contain("fixture-code"));
        Assert.That(response.Headers.CacheControl!.NoStore, Is.True);
        Assert.That(f.Forms.Single()["redirect_uri"], Is.EqualTo(connect.GetString("redirect_uri")));
        Assert.That(f.Ext.Credentials.Load("alice"), Is.Not.Null);
        Assert.That(f.Ext.Credentials.Load("bob"), Is.Null);
        Assert.That(f.Host.Feature.Providers["openai"], Is.TypeOf<OpenAiSubscriptionProvider>());
        Assert.That(f.Ext.Flows.HasPending("alice"), Is.False);
        using var replay = await http.GetAsync(f.Callback());
        Assert.That(replay.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(f.TokenCalls, Is.EqualTo(1));
    }
    [Test]
    public async Task Occupied_callback_port_selects_an_available_loopback_port_before_authorization()
    {
        using var occupied = new TcpListener(IPAddress.Loopback, 0); occupied.Start();
        var port = ((IPEndPoint)occupied.LocalEndpoint).Port;
        using var f = new Fixture();
        f.Ext.Options.RedirectUri = $"http://127.0.0.1:{port}/auth/callback";
        var result = await StartAutomatic(f);
        var redirect = new Uri(result.GetString("redirect_uri")!);
        Assert.That(redirect.Host, Is.EqualTo("127.0.0.1"));
        Assert.That(redirect.Port, Is.Not.EqualTo(port).And.GreaterThan(0));
        using var http = new HttpClient();
        using var response = await http.GetAsync(f.Callback());
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(f.Forms.Single()["redirect_uri"], Is.EqualTo(redirect.AbsoluteUri));
    }
    [TestCase("state"), TestCase("duplicate"), TestCase("denied"), TestCase("expired"), TestCase("disconnect"), TestCase("new-login")]
    public async Task Automatic_receiver_rejects_invalid_or_cancelled_callbacks_without_exchange(string kind)
    {
        using var f = new Fixture(); f.Ext.Options.RedirectUri = "http://127.0.0.1:0/auth/callback";
        await StartAutomatic(f); var url = f.Callback();
        switch (kind)
        {
            case "state": url = f.Callback("wrong"); break;
            case "duplicate": url += "&state=other"; break;
            case "denied": url = f.Authorization["redirect_uri"] + "?error=access_denied&state=" + f.Authorization["state"]; break;
            case "expired": f.Host.Clock.Now += TimeSpan.FromMinutes(11); break;
            case "disconnect": f.Ext.Flows.Disconnect("alice"); break;
            case "new-login": await StartAutomatic(f); break;
        }
        using var http = new HttpClient(); using var response = await http.GetAsync(url);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(f.TokenCalls, Is.Zero); Assert.That(f.Ext.Credentials.Load("alice"), Is.Null);
        if (kind == "denied")
        {
            var status = (JsonObject)(await f.Host.SendAsync("GET", "/ext/openai_auth/status", "alice"))!;
            Assert.That(status.GetString("callback_error"), Does.Contain("not authorized"));
            Assert.That(status.GetBool("pending"), Is.False);
            Assert.That(f.Ext.Flows.CallbackError("bob"), Is.Null);
            await StartAutomatic(f); Assert.That(f.Ext.Flows.CallbackError("alice"), Is.Null);
        }
    }
    [Test]
    public async Task Host_shutdown_releases_callback_port()
    {
        using var f = new Fixture(); f.Ext.Options.RedirectUri = "http://127.0.0.1:0/auth/callback";
        var connect = await StartAutomatic(f); var port = new Uri(connect.GetString("redirect_uri")!).Port;
        f.Host.Feature.RunShutdownHandlers();
        using var replacement = new TcpListener(IPAddress.Loopback, port); replacement.Start();
        Assert.That(f.Ext.Flows.HasPending("alice"), Is.False);
    }
    [Test]
    public async Task Automatic_hosted_callback_cannot_select_another_identity()
    {
        using var f = new Fixture("/chat");
        f.Ext.Options.RedirectUri = "https://app.example/chat/ext/openai_auth/callback";
        await StartAutomatic(f);
        Assert.ThrowsAsync<HttpError>(async () => await f.Ext.Flows.AutomaticCallbackAsync(f.Callback(), CancellationToken.None, "bob"));
        Assert.That(f.TokenCalls, Is.Zero);
        await f.Ext.Flows.AutomaticCallbackAsync(f.Callback(), CancellationToken.None, "alice");
        Assert.That(f.Ext.Credentials.Load("alice"), Is.Not.Null);
    }
}
