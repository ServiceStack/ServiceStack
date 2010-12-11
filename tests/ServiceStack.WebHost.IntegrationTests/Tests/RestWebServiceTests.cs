using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class RestWebServiceTests
		: RestsTestBase
	{

		[Test]
		public void Can_call_EchoRequest_with_AcceptAll()
		{
			var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One", "*/*");
			AssertResponse<EchoRequest>(response, null, x =>
			{
				Assert.That(x.Id, Is.EqualTo(1));
				Assert.That(x.String, Is.EqualTo("One"));
			});
		}

		[Test]
		public void Can_call_EchoRequest_with_AcceptJson()
		{
			var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One", ContentType.Json);
			AssertResponse<EchoRequest>(response, ContentType.Json, x =>
			{
				Assert.That(x.Id, Is.EqualTo(1));
				Assert.That(x.String, Is.EqualTo("One"));
			});
		}

		[Test]
		public void Can_call_EchoRequest_with_AcceptXml()
		{
			var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One", ContentType.Xml);
			AssertResponse<EchoRequest>(response, ContentType.Xml, x =>
			{
				Assert.That(x.Id, Is.EqualTo(1));
				Assert.That(x.String, Is.EqualTo("One"));
			});
		}

		[Test]
		public void Can_call_EchoRequest_with_AcceptJsv()
		{
			var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One", ContentType.Jsv);
			AssertResponse<EchoRequest>(response, ContentType.Jsv, x =>
			{
				Assert.That(x.Id, Is.EqualTo(1));
				Assert.That(x.String, Is.EqualTo("One"));
			});
		}

		[Test]
		public void Can_call_EchoRequest_with_QueryString()
		{
			var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One?Long=2&Bool=True", ContentType.Json);
			AssertResponse<EchoRequest>(response, ContentType.Json, x =>
			{
				Assert.That(x.Id, Is.EqualTo(1));
				Assert.That(x.String, Is.EqualTo("One"));
				Assert.That(x.Long, Is.EqualTo(2));
				Assert.That(x.Bool, Is.EqualTo(true));
			});
		}

		[Test]
		public void Can_call_WildCardRequest_with_alternate_WildCard_defined()
		{
			var response = GetWebResponse(ServiceClientBaseUri + "/wildcard/1/aPath/edit", ContentType.Json);
			AssertResponse<WildCardRequest>(response, ContentType.Json, x =>
			{
				Assert.That(x.Id, Is.EqualTo(1));
				Assert.That(x.Path, Is.EqualTo("aPath"));
				Assert.That(x.Action, Is.EqualTo("edit"));
				Assert.That(x.RemainingPath, Is.Null);
			});
		}

		[Test]
		public void Can_call_WildCardRequest_WildCard_mapping()
		{
			var response = GetWebResponse(ServiceClientBaseUri + "/wildcard/1/remaining/path/to/here", ContentType.Json);
			AssertResponse<WildCardRequest>(response, ContentType.Json, x =>
			{
				Assert.That(x.Id, Is.EqualTo(1));
				Assert.That(x.Path, Is.Null);
				Assert.That(x.Action, Is.Null);
				Assert.That(x.RemainingPath, Is.EqualTo("remaining/path/to/here"));
			});
		}

		[Test]
		public void Can_call_WildCardRequest_WildCard_mapping_with_QueryString()
		{
			var response = GetWebResponse(ServiceClientBaseUri + "/wildcard/1/remaining/path/to/here?Action=edit", ContentType.Json);
			AssertResponse<WildCardRequest>(response, ContentType.Json, x =>
			{
				Assert.That(x.Id, Is.EqualTo(1));
				Assert.That(x.Path, Is.Null);
				Assert.That(x.Action, Is.EqualTo("edit"));
				Assert.That(x.RemainingPath, Is.EqualTo("remaining/path/to/here"));
			});
		}

	}

}