#nullable enable
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.AiTests;

public class McpClientTransportTests
{
    [TestCase(false, false, "2025-06-18")] [TestCase(true, false, "2025-06-18")]
    [TestCase(false, true, "2025-06-18")] [TestCase(true, true, "2025-06-18")]
    [TestCase(false, false, "2026-07-28")] [TestCase(true, false, "2026-07-28")]
    [TestCase(false, true, "2026-07-28")] [TestCase(true, true, "2026-07-28")]
    public async Task Sdk_connects_to_older_streamable_http_servers_and_disposes(bool streaming, bool inputRequired, string protocol)
    {
        using var reserve = new TcpListener(IPAddress.Loopback, 0);
        reserve.Start(); var port = ((IPEndPoint)reserve.LocalEndpoint).Port; reserve.Stop();
        using var listener = new HttpListener();
        var endpoint = new Uri($"http://127.0.0.1:{port}/mcp/");
        listener.Prefixes.Add(endpoint.AbsoluteUri); listener.Start();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var calls = 0;
        var callsWithHeader = 0;
        var serving = Task.Run(async () => {
            try {
                while (!stop.IsCancellationRequested) {
                    var context = await listener.GetContextAsync().WaitAsync(stop.Token);
                    if (context.Request.HttpMethod != "POST") { context.Response.StatusCode = 405; context.Response.Close(); continue; }
                    using var reader = new StreamReader(context.Request.InputStream);
                    var body = JsonNode.Parse(await reader.ReadToEndAsync())!.AsObject();
                    var id = body["id"];
                    if (id == null) { context.Response.StatusCode = 202; context.Response.Close(); continue; }
                    JsonNode? result = body["method"]?.GetValue<string>() switch {
                        "server/discover" when protocol == "2026-07-28" => JsonNode.Parse("""{"supportedVersions":["2026-07-28"],"capabilities":{"tools":{}}}"""),
                        "initialize" => JsonNode.Parse("""{"protocolVersion":"2025-06-18","capabilities":{"tools":{}},"serverInfo":{"name":"fixture","version":"1"}}"""),
                        "tools/list" => JsonNode.Parse("""{"tools":[{"name":"echo","inputSchema":{"type":"object","properties":{"owner":{"type":"string","x-mcp-header":"owner"}},"required":["owner"]}}]}"""),
                        "tools/call" when inputRequired => JsonNode.Parse("""{"resultType":"input_required","requestState":"continue-me"}"""),
                        "tools/call" => JsonNode.Parse("""{"content":[{"type":"text","text":"fixture-result"}]}"""),
                        _ => null,
                    };
                    if (body["method"]?.GetValue<string>() == "tools/call") {
                        calls++;
                        if (context.Request.Headers["Mcp-Param-owner"] == "ServiceStack") callsWithHeader++;
                    }
                    var response = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id.DeepClone() };
                    if (result == null) response["error"] = new JsonObject { ["code"] = -32601, ["message"] = "Method not found" };
                    else response["result"] = result;
                    var json = response.ToJsonString();
                    context.Response.ContentType = streaming ? "text/event-stream" : "application/json";
                    var bytes = Encoding.UTF8.GetBytes(streaming ? "event: message\ndata: " + json + "\n\n" : json);
                    await context.Response.OutputStream.WriteAsync(bytes); context.Response.Close();
                }
            } catch (OperationCanceledException) { } catch (HttpListenerException) { }
        });
        try {
            var extension = new McpClientExtension();
            extension.NetworkPolicy.AllowHttp = true; extension.NetworkPolicy.AllowedPorts.Add(port);
            extension.NetworkPolicy.AllowPrivateAddress = (host, ip) => host == "127.0.0.1" && IPAddress.IsLoopback(ip);
            var factory = new McpClientSessionFactory(extension);
            await using var session = await factory.CreateAsync(new McpClientServer { Id = "fixture", Endpoint = endpoint }, null, null, stop.Token);
            Assert.That(session.ProtocolVersion, Is.EqualTo(protocol));
            var list = await session.ListToolsAsync(null, stop.Token);
            Assert.That(list["tools"]!.AsArray().Count, Is.EqualTo(1));
            if (inputRequired) {
                var timer = System.Diagnostics.Stopwatch.StartNew();
                try { await session.CallAsync("echo", new JsonObject { ["owner"] = "ServiceStack" }, stop.Token); Assert.Fail("Unsupported interaction must fail"); }
                catch (Exception e) when (e is not AssertionException) {
                    Assert.That(e.ToString(), Does.Contain("not supported").Or.Contain("not replayed"));
                }
                Assert.That(timer.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
            } else {
                var result = await session.CallAsync("echo", new JsonObject { ["owner"] = "ServiceStack" }, stop.Token);
                Assert.That(result["content"]![0]!["text"]!.GetValue<string>(), Is.EqualTo("fixture-result"));
            }
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(callsWithHeader, Is.EqualTo(1));
        } finally { stop.Cancel(); listener.Stop(); await serving; }
    }
}
