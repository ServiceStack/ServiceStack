using System.IO;
using System.Web.UI;
using NUnit.Framework;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.WebHost.Endpoints.Support.Metadata.Controls;
using ServiceStack.WebHost.Endpoints.Tests.Support;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class OperationTests : MetadataTestBase
	{
	    private AppHost _appHost;
	    private OperationControl _operationControl;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            _appHost = new AppHost();
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
            _appHost.Config.WebHostUrl = null;

            var stringWriter = new StringWriter();
            _operationControl.Render(new HtmlTextWriter(stringWriter));

            string html = stringWriter.ToString();
            Assert.IsTrue(html.Contains("<a href=\"/metadata\">&lt;back to all web services</a>"));
        }

	}
}