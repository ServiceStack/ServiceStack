using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;

#nullable enable

namespace ServiceStack.Extensions.Tests;

[TestFixture]
public class PdfRendererApiTests
{
    [Test]
    public async Task Option_and_stream_apis_work_with_custom_renderers()
    {
        IPdfRenderer renderer = new CustomRenderer();
        var options = new PdfRenderOptions { Language = "de", Region = "DE", ["custom"] = 1 };

        Assert.That(options[PdfRenderOptions.LanguageKey], Is.EqualTo("de"));
        Assert.That(options[PdfRenderOptions.RegionKey], Is.EqualTo("DE"));
        options.Region = null;
        Assert.That(options.ContainsKey(PdfRenderOptions.RegionKey), Is.False);

        Assert.That(await renderer.RenderAsync("invoice", "{\"id\":1}", options),
            Is.EqualTo(new byte[] { 1, 2, 3 }));

        await using var stream = new MemoryStream();
        await renderer.RenderToStreamAsync("invoice", stream, "{\"id\":1}", options);
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    class CustomRenderer : IPdfRenderer
    {
        public bool IsAvailable => true;
        public List<string> GetTemplateNames() => ["invoice"];
        public string ResolvePath(string name, string ext = ".typ", bool mustExist = true) => name + ext;
        public Task<byte[]> RenderAsync(string name, string? dataJson = null, PdfRenderOptions? options = null,
            CancellationToken token = default) => Task.FromResult(new byte[] { 1, 2, 3 });
        public Task<byte[]> RenderPngAsync(string name, string? dataJson = null, int page = 1, int? ppi = null,
            CancellationToken token = default) => Task.FromResult(new byte[] { 4, 5, 6 });
    }
}
