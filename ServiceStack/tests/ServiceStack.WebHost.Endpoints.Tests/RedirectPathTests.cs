using System.Reflection;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class RedirectPathTests
{
    class AppHost() : AppSelfHostBase(nameof(RedirectPathTests), typeof(RedirectPathTests).Assembly)
    {
        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                DefaultRedirectPath = "~/does-resolve"
            });
        }

        public override string ResolveAbsoluteUrl(string virtualPath, IRequest httpReq)
        {
            return virtualPath == "~/does-resolve"
                ? base.ResolveAbsoluteUrl("~/webpage.html", httpReq)
                : base.ResolveAbsoluteUrl(virtualPath, httpReq);
        }
    }

    private readonly ServiceStackHost appHost;

    public RedirectPathTests()
    {
        appHost = new AppHost()
            .Init()
            .Start(Config.ListeningOn);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    [Test]
    public void DefaultRedirectPath_RelativeUrl_does_resolve()
    {
        var html = Config.ListeningOn.GetStringFromUrl();
        Assert.That(html, Does.Contain("Default index"));
    }
}