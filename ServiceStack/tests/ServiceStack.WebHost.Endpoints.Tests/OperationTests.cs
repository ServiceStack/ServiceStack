using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;
using Funq;
using NUnit.Framework;
using ServiceStack.Common.Tests;
using ServiceStack.Metadata;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class OperationTestsAppHost() : AppHostHttpListenerBase(nameof(GetCustomer), typeof(GetCustomer).Assembly)
{
    public override void Configure(Container container) { }
}

[TestFixture]
public class OperationTests : IService
{
    private OperationTestsAppHost appHost;
    private OperationControl operationControl;

    [OneTimeSetUp]
    public void OnTestFixtureSetUp()
    {
        appHost = new OperationTestsAppHost();
        appHost.Init();
#if !NETFRAMEWORK
        appHost.Start(Config.ListeningOn);
#endif

        var dummyServiceType = GetType();
        appHost.Metadata.Add(dummyServiceType, typeof(GetCustomer), typeof(GetCustomerResponse));
        appHost.Metadata.Add(dummyServiceType, typeof(GetCustomers), typeof(GetCustomersResponse));
        appHost.Metadata.Add(dummyServiceType, typeof(StoreCustomer), null);

        operationControl = new OperationControl
        {
            HttpRequest = new MockHttpRequest {PathInfo = "", RawUrl = "http://localhost:4444/metadata"},
            MetadataConfig = ServiceEndpointsMetadataConfig.Create(""),
            Format = Format.Json,
            HostName = "localhost",
            RequestMessage = "(string)",
            ResponseMessage = "(HttpWebResponse)",
            Title = "Metadata page",
            OperationName = "operationname",
            MetadataHtml = "<p>Operation</p>",
        };
    }

    [OneTimeTearDown]
    public void OnTestFixtureTearDown()
    {
        appHost.Dispose();
    }

    [TearDown]
    public void OnTearDown()
    {
        if (appHost?.Config?.WebHostUrl != null)
            appHost.Config.WebHostUrl = null;
    }

    [Test]
    public async Task OperationControl_render_creates_link_back_to_main_page_using_WebHostUrl_when_set()
    {
        appHost.Config.WebHostUrl = "https://host.example.com/_api";

        using var ms = MemoryStreamFactory.GetStream();
        await operationControl.RenderAsync(ms);
    
        string html = await ms.ReadToEndAsync();
        Assert.IsTrue(html.Contains("<a href=\"https://host.example.com/_api/metadata\">&lt;back to all web services</a>"));
    }

    [Test]
    public async Task OperationControl_render_creates_link_back_to_main_page_using_relative_uri_when_WebHostUrl_not_set()
    {
        using var ms = MemoryStreamFactory.GetStream();
        await operationControl.RenderAsync(ms);
        string html = await ms.ReadToEndAsync();
        Assert.That(html, Does.Contain("<a href=\"http://localhost/metadata\">&lt;back to all web services</a>"));
    }

    [Test]
    public void When_culture_is_turkish_operations_containing_capital_I_are_still_visible()
    {
        appHost.Metadata.Add(GetType(), typeof(HelloImage), null);

        using (new CultureSwitch("tr-TR"))
        {
            Assert.IsTrue(appHost.Metadata.IsVisible(operationControl.HttpRequest, Format.Json, "HelloImage"));
        }
    }
}

[DataContract]
public class HelloImage
{
}

public class CultureSwitch : IDisposable
{
    private readonly CultureInfo _currentCulture;

    public CultureSwitch(string culture)
    {
#if !NETFRAMEWORK
        _currentCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
#else
            var currentThread = Thread.CurrentThread;
            _currentCulture = currentThread.CurrentCulture;
            var switchCulture = CultureInfo.GetCultureInfo(culture);
            currentThread.CurrentCulture = switchCulture;
#endif
    }

    public void Dispose()
    {
#if !NETFRAMEWORK
        CultureInfo.CurrentCulture = _currentCulture;
#else
            Thread.CurrentThread.CurrentCulture = _currentCulture;
#endif
    }
}