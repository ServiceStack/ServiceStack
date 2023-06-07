using System.Linq;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Testing;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class ModuleTests
{
    private readonly ServiceStackHost appHost;
    private readonly IVirtualPathProvider ssResources;
    public ModuleTests()
    {
        appHost = new BasicAppHost().Init();
        ssResources = appHost.GetVirtualFileSources()
            .FirstOrDefault(x => x is ResourceVirtualFiles { RootNamespace: nameof(ServiceStack) });
    }

    [OneTimeTearDown] public void OneTimeTearDown() => appHost.Dispose();

    [Test]
    public void Can_search_modules_resources_folder()
    {
        var uiIndexFile = ssResources.GetFile("/modules/ui/index.html");
        Assert.That(uiIndexFile, Is.Not.Null);

        var sharedComponentFiles = ssResources.GetAllMatchingFiles("/modules/shared/*.html").ToList();
        Assert.That(sharedComponentFiles.Count, Is.GreaterThanOrEqualTo(4));

        var componentFiles = ssResources.GetAllMatchingFiles("/modules/ui/components/*.mjs").ToList();
        Assert.That(componentFiles.Count, Is.GreaterThanOrEqualTo(6));

        var adminUiJsFiles = ssResources.GetAllMatchingFiles("/modules/admin-ui/lib/*.mjs").ToList();
        Assert.That(adminUiJsFiles.Count, Is.GreaterThanOrEqualTo(2));

        var adminUiMjsFiles = ssResources.GetAllMatchingFiles("/modules/admin-ui/components/*.mjs").ToList();
        Assert.That(adminUiMjsFiles.Count, Is.GreaterThanOrEqualTo(6));
    }

    [Test]
    public void Tailwind_did_gen_properly()
    {
        var uiCss = ssResources.GetFile("/css/ui.css");
        Assert.That(uiCss, Is.Not.Null);

        var uiCssContents = uiCss.ReadAllText();
        Assert.That(uiCssContents, Does.Contain("col-span-6"));
    }
}