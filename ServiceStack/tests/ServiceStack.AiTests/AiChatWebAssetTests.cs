#nullable enable
#if NET8_0_OR_GREATER
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.AiTests;

[NonParallelizable]
public class AiChatWebAssetTests
{
    class AppHost() : AppHostBase(nameof(AiChatWebAssetTests), typeof(ChatFeature).Assembly)
    {
        public override void Configure() { }
    }

    WebApplication app = null!;
    HttpClient client = null!;
    ChatFeature feature = null!;
    string dataPath = null!;

    [OneTimeSetUp]
    public async Task Start()
    {
        dataPath = Path.Combine(Path.GetTempPath(), "chat-web-assets-" + Guid.NewGuid().ToString("N"));
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddServiceStack(Array.Empty<System.Reflection.Assembly>());
        feature = new ChatFeature { RequireAuth = false, AppDataPath = dataPath, DisableExtensions = ["app", "gallery", "gemini", "analytics"] };
        feature.Routes.AddGet("/test/json", _ => Task.FromResult<object?>(new JsonObject { ["text"] = new string('x', 4096) }));
        feature.Routes.AddGet("/test/stream", _ => Task.FromResult<object?>(new ChatStreamResult(async res => {
            res.ContentType = "text/event-stream";
            await res.WriteAsync("data: " + new string('x', 4096) + "\n\n");
        })));
        feature.Routes.AddGet("/test/binary", _ => Task.FromResult<object?>(new ChatResult {
            Body = new byte[4096], ContentType = "application/octet-stream" }));
        builder.Services.AddPlugin(feature);
        app = builder.Build();
        app.UseServiceStack(new AppHost(), options => options.MapEndpoints());
        await app.StartAsync();
        client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
    }

    [OneTimeTearDown]
    public async Task Stop()
    {
        client?.Dispose();
        if (app != null) { await app.StopAsync(); await app.DisposeAsync(); }
        if (Directory.Exists(dataPath)) Directory.Delete(dataPath, true);
    }

    [TestCase("/chat/ui/lib/vue.min.mjs", "gzip")]
    [TestCase("/chat/ext/core_tools/index.mjs", "deflate")]
    [TestCase("/chat/test/json", "gzip")]
    public async Task Buffered_assets_and_JSON_negotiate_compression(string path, string encoding)
    {
        var identity = await client.GetByteArrayAsync(path);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("Accept-Encoding", encoding);
        using var response = await client.SendAsync(request);
        Assert.That(response.IsSuccessStatusCode, Is.True);
        Assert.That(response.Content.Headers.ContentEncoding.Single(), Is.EqualTo(encoding));
        Assert.That(response.Headers.Vary, Does.Contain("Accept-Encoding"));
        var compressed = await response.Content.ReadAsByteArrayAsync();
        using var input = new MemoryStream(compressed);
        using Stream decoder = encoding == "gzip" ? new GZipStream(input, CompressionMode.Decompress) : new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        await decoder.CopyToAsync(output);
        Assert.That(output.ToArray(), Is.EqualTo(identity));
        Assert.That(compressed.Length, Is.LessThan(identity.Length));
    }

    [TestCase("gzip;q=0, deflate;q=0, *;q=0", null)]
    [TestCase("gzip;q=0.2, deflate;q=0.8", "deflate")]
    [TestCase("gzip;q=0, *;q=1", "deflate")]
    public async Task Compression_respects_quality_and_explicit_exclusions(string accept, string? expected)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/chat/test/json");
        request.Headers.TryAddWithoutValidation("Accept-Encoding", accept);
        using var response = await client.SendAsync(request);
        Assert.That(response.Content.Headers.ContentEncoding.SingleOrDefault(), Is.EqualTo(expected));
    }

    [TestCase("/chat/ui/index.mjs")]
    // pdf/pages.mjs isn't used as the pdf extension is disabled when the typst CLI isn't installed, e.g. in CI
    [TestCase("/chat/ext/core_tools/pages.mjs")]
    public async Task Assets_revalidate_with_weak_strong_list_and_star_etags(string path)
    {
        using var original = await client.GetAsync(path);
        Assert.That(original.Headers.CacheControl?.NoCache, Is.True);
        var tag = original.Headers.ETag!.ToString();
        foreach (var match in new[] { tag, tag[2..], "\"unrelated\", " + tag, "*" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.TryAddWithoutValidation("If-None-Match", match);
            using var response = await client.SendAsync(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotModified), match);
            Assert.That(await response.Content.ReadAsByteArrayAsync(), Is.Empty);
            Assert.That(response.Headers.ETag!.ToString(), Is.EqualTo(tag));
        }
        using var stale = new HttpRequestMessage(HttpMethod.Get, path);
        stale.Headers.TryAddWithoutValidation("If-None-Match", "W/\"stale\"");
        using var refreshed = await client.SendAsync(stale);
        Assert.That(refreshed.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Head_has_get_headers_and_no_body()
    {
        using var get = await client.GetAsync("/chat/ui/index.mjs");
        using var request = new HttpRequestMessage(HttpMethod.Head, "/chat/ui/index.mjs");
        using var head = await client.SendAsync(request);
        Assert.That(head.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(head.Headers.ETag!.ToString(), Is.EqualTo(get.Headers.ETag!.ToString()));
        Assert.That(head.Content.Headers.ContentLength, Is.EqualTo(get.Content.Headers.ContentLength));
        Assert.That(await head.Content.ReadAsByteArrayAsync(), Is.Empty);
    }

    [Test]
    public async Task Event_streams_and_binary_assets_are_not_compressed()
    {
        foreach (var path in new[] { "/chat/test/stream", "/chat/test/binary" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip");
            using var response = await client.SendAsync(request);
            Assert.That(response.Content.Headers.ContentEncoding, Is.Empty);
            if (path.EndsWith("stream"))
                Assert.That(await response.Content.ReadAsStringAsync(), Does.StartWith("data: "));
            else
                Assert.That((await response.Content.ReadAsByteArrayAsync()).Length, Is.EqualTo(4096));
        }
    }

    [TestCase("")]
    [TestCase("/chat")]
    [TestCase("/nested/chat")]
    public async Task Index_preloads_resolve_mount_prefix_and_omit_eager_editor_scripts(string prefix)
    {
        var previous = feature.RoutePrefix;
        try
        {
            feature.RoutePrefix = prefix;
            var result = (ChatResult)(await feature.IndexHandlerAsync(new BasicRequest()))!;
            var html = result.Text!;
            Assert.That(html, Does.Contain($"rel=\"modulepreload\" href=\"{prefix}{feature.ImportMaps["vue"]}\""));
            Assert.That(html, Does.Contain($"import('{prefix}/ui/index.mjs')"));
            Assert.That(html, Does.Contain("id=\"llms-loading-sprite\""));
            Assert.That(html, Does.Contain("href=\"#llms-loading-sprite\""));
            Assert.That(html, Does.Not.Contain("/ui/loading.svg"));
            Assert.That(html, Does.Not.Contain("codemirror.js"));
            Assert.That(html, Does.Not.Contain("<!-- modulepreloads -->"));
        }
        finally { feature.RoutePrefix = previous; }
    }

    [Test]
    public async Task Transformed_asset_etag_describes_served_content()
    {
        var previous = feature.RoutePrefix;
        try
        {
            var ctx = new ChatRequestContext(feature, new BasicRequest(), new());
            var first = (ChatResult)(await feature.ServeEmbeddedFileAsync(ctx, "chat/ui", "ai.mjs"))!;
            Assert.That(Encoding.UTF8.GetString(first.Body!), Does.Contain("const base = '/chat'"));
            feature.RoutePrefix = "/other";
            var second = (ChatResult)(await feature.ServeEmbeddedFileAsync(ctx, "chat/ui", "ai.mjs"))!;
            Assert.That(Encoding.UTF8.GetString(second.Body!), Does.Contain("const base = '/other'"));
            Assert.That(second.Headers!["ETag"], Is.Not.EqualTo(first.Headers!["ETag"]));
        }
        finally { feature.RoutePrefix = previous; }
    }

    [TestCase("../index.html")]
    [TestCase("/index.html")]
    [TestCase("..\\index.html")]
    [TestCase("C:/index.html")]
    public async Task Asset_paths_cannot_escape_their_virtual_directory(string path)
    {
        var ctx = new ChatRequestContext(feature, new BasicRequest(), new());
        var result = (ChatResult)(await feature.ServeEmbeddedFileAsync(ctx, "chat/ui", path))!;
        Assert.That(result.Status, Is.EqualTo(404));
    }
}
#endif
