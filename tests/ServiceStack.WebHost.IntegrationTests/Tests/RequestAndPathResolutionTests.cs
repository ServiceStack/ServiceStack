using NUnit.Framework;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class RequestAndPathResolutionTests
		: TestsBase
	{
		[SetUp]
		public void OnBeforeTest()
		{
			EndpointHandlerBase.ServiceManager = new ServiceManager(true, typeof(ReverseService).Assembly); 
		}

		protected override IServiceClient CreateNewServiceClient()
		{
			return new DirectServiceClient(EndpointHandlerBase.ServiceManager);
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