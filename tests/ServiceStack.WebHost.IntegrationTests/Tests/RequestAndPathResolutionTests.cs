using System.Reflection;
using NUnit.Framework;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.IntegrationTests.Services;
using ServiceStack.WebHost.IntegrationTests.Testing;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class RequestAndPathResolutionTests
		: TestsBase
	{
		public RequestAndPathResolutionTests()
			: base(typeof(ReverseService).Assembly)
		{
		}

		[SetUp]
		public void OnBeforeTest()
		{
			base.OnBeforeEachTest();
		}

		[Test]
		public void Can_process_default_request()
		{
			var request = (EchoRequest)ExecutePath("/Xml/SyncReply/EchoRequest");
			Assert.That(request, Is.Not.Null);
		}

		[Test]
		public void Can_process_default_case_insensitive_request()
		{
			var request = (EchoRequest)ExecutePath("/xml/syncreply/echorequest");
			Assert.That(request, Is.Not.Null);
		}

		[Test]
		public void Can_process_default_request_with_queryString()
		{
			var request = (EchoRequest)ExecutePath("/Xml/SyncReply/EchoRequest?Id=1&String=Value");
			Assert.That(request, Is.Not.Null);
			Assert.That(request.Id, Is.EqualTo(1));
			Assert.That(request.String, Is.EqualTo("Value"));
		}
	}
}