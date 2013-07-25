using System.IO;
using System.Web.UI;
using Funq;
using NUnit.Framework;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.WebHost.Endpoints.Support.Metadata.Controls;
using ServiceStack.WebHost.Endpoints.Tests.Support;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class OperationTestsAppHost : AppHostHttpListenerBase
    {
        public OperationTestsAppHost() : base(typeof(GetCustomer).Name, typeof(GetCustomer).Assembly) { }
        public override void Configure(Container container) { }
    }

	[TestFixture]
	public class OperationTests : MetadataTestBase
	{
        private OperationTestsAppHost _appHost;
	    private OperationControl _operationControl;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            _appHost = new OperationTestsAppHost();
            _appHost.Init();

            _operationControl = new OperationControl
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
            _appHost.Dispose();
        }

        [TearDown]
        public void OnTearDown()
        {
            _appHost.Config.WebHostUrl = null;
        }

        [Test]
        public void OperationControl_render_creates_link_back_to_main_page_using_WebHostUrl_when_set()
        {
            _appHost.Config.WebHostUrl = "https://host.example.com/_api";

            var stringWriter = new StringWriter();
            _operationControl.Render(new HtmlTextWriter(stringWriter));

            string html = stringWriter.ToString();
            Assert.IsTrue(html.Contains("<a href=\"https://host.example.com/_api/metadata\">&lt;back to all web services</a>"));
        }

        [Test]
        public void OperationControl_render_creates_link_back_to_main_page_using_relative_uri_when_WebHostUrl_not_set()
        {
            var stringWriter = new StringWriter();
            _operationControl.Render(new HtmlTextWriter(stringWriter));

            string html = stringWriter.ToString();
            Assert.IsTrue(html.Contains("<a href=\"/metadata\">&lt;back to all web services</a>"));
        }

	}
}