using System.Linq;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.Testing;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class ModuleTests
{
    [Test]
    public void Can_search_modules_resources_folder()
    {
        using var appHost = new BasicAppHost().Init();
        var ssResources = appHost.GetVirtualFileSources()
            .FirstOrDefault(x => x is ResourceVirtualFiles rvfs && rvfs.RootNamespace == nameof(ServiceStack));
        Assert.That(ssResources, Is.Not.Null);

        var uiIndexFile = ssResources.GetFile("/modules/ui/index.html");
        Assert.That(uiIndexFile, Is.Not.Null);

        var componentFiles = ssResources.GetAllMatchingFiles("/modules/ui/components/*.html").ToList();
        Assert.That(componentFiles.Count, Is.GreaterThanOrEqualTo(7));
    }

}