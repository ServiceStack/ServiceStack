using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Web.UI;
using Funq;
using NUnit.Framework;
using ServiceStack.Common.Tests;
using ServiceStack.Metadata;
using ServiceStack.Testing;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class OperationTestsAppHost : AppHostHttpListenerBase
    {
        public OperationTestsAppHost() : base(typeof(GetCustomer).Name, typeof(GetCustomer).Assembly) { }
        public override void Configure(Container container) { }
    }

	[TestFixture]
	public class OperationTests : IService
	{
        private OperationTestsAppHost appHost;
	    private OperationControl operationControl;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new OperationTestsAppHost();
            appHost.Init();

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
                MetadataHtml = "<p>Operation</p>"
            };
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [TearDown]
        public void OnTearDown()
        {
            appHost.Config.WebHostUrl = null;
        }

        [Test]
        public void OperationControl_render_creates_link_back_to_main_page_using_WebHostUrl_when_set()
        {
            appHost.Config.WebHostUrl = "https://host.example.com/_api";

            var stringWriter = new StringWriter();
            operationControl.Render(new HtmlTextWriter(stringWriter));

            string html = stringWriter.ToString();
            Assert.IsTrue(html.Contains("<a href=\"https://host.example.com/_api/metadata\">&lt;back to all web services</a>"));
        }

        [Test]
        public void OperationControl_render_creates_link_back_to_main_page_using_relative_uri_when_WebHostUrl_not_set()
        {
            var stringWriter = new StringWriter();
            operationControl.Render(new HtmlTextWriter(stringWriter));

            string html = stringWriter.ToString();
            Assert.That(html, Is.StringContaining("<a href=\"metadata\">&lt;back to all web services</a>"));
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
            var currentThread = Thread.CurrentThread;
            _currentCulture = currentThread.CurrentCulture;
            var switchCulture = CultureInfo.GetCultureInfo(culture);
            currentThread.CurrentCulture = switchCulture;
        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = _currentCulture;
        }
    }
}