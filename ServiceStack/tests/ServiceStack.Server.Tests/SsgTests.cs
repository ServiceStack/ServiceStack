#nullable enable
#if NET6_0_OR_GREATER

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NUnit.Framework;
using ServiceStack.Mvc;

namespace ServiceStack.Server.Tests;

[TestFixture]
public class SsgTests
{
    public class TestPageModel : PageModel
    {
        public string Slug { get; set; }
        public int? Page { get; set; }

        public int Counter = 0;
        public async Task OnGetAsync()
        {
            Counter++;
        }
    }
    
    [Test]
    public void Can_resolve_custom_route()
    {
        Assert.That(RazorSsg.ResolvePageRoute("/test", new TestPageModel()), Is.EqualTo("/test.html"));
        Assert.That(RazorSsg.ResolvePageRoute("/test/{Slug}", new TestPageModel { Slug = "foo" }), Is.EqualTo("/test/foo.html"));
        Assert.That(RazorSsg.ResolvePageRoute("/test/{slug}", new TestPageModel { Slug = "foo" }), Is.EqualTo("/test/foo.html"));
        Assert.That(RazorSsg.ResolvePageRoute("/test/{slug}/", new TestPageModel { Slug = "foo" }), Is.EqualTo("/test/foo/index.html"));
        Assert.That(RazorSsg.ResolvePageRoute("/test/{Slug}_{Page}", new TestPageModel { Slug = "foo", Page = 2 }), Is.EqualTo("/test/foo_2.html"));
        Assert.That(RazorSsg.ResolvePageRoute("/test/{slug}_{page}", new TestPageModel { Slug = "foo", Page = 2 }), Is.EqualTo("/test/foo_2.html"));
        Assert.That(RazorSsg.ResolvePageRoute("/test/{slug}_{page}/", new TestPageModel { Slug = "foo", Page = 2 }), Is.EqualTo("/test/foo_2/index.html"));
    }
    
    public class TestGet : PageModel
    {
        public int Counter = 0;
        public void OnGet()
        {
            Counter++;
        }
    }

    [Test]
    public async Task Can_call_OnGetAsync_PageModel()
    {
        var model = new TestPageModel();
        var fn = RazorSsg.ResolveOnGetAsync(model.GetType());
        Assert.That(fn, Is.Not.Null);
        await fn(model);
        Assert.That(model.Counter, Is.EqualTo(1));
    }

    [Test]
    public async Task Can_call_OnGet_PageModel()
    {
        var model = new TestGet();
        var fn = RazorSsg.ResolveOnGetAsync(model.GetType());
        Assert.That(fn, Is.Not.Null);
        await fn(model);
        Assert.That(model.Counter, Is.EqualTo(1));
    }
}

#endif