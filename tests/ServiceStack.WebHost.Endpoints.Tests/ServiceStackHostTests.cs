using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class ServiceStackHostTests
	{
		[Test]
		public void Can_run_nested_service()
		{
			var host = new TestAppHost();
			host.Init();

			var request = new Nested();
			var response = host.ExecuteService(request) as NestedResponse;

			Assert.That(response, Is.Not.Null);
		}

		[Test]
		public void Can_run_test_service()
		{
			var host = new TestAppHost();
			host.Init();

			var request = new Test();
			var response = host.ExecuteService(request) as TestResponse;

			Assert.That(response, Is.Not.Null);
			Assert.That(response.Foo, Is.Not.Null);
		}

		[Test]
		public void Call_AsyncOneWay_endpoint_on_TestService_calls_Execute()
		{
			var host = new TestAppHost();
			host.Init();

			TestService.ResetStats();

			var request = new Test();
			var response = host.ExecuteService(request, EndpointAttributes.OneWay) as TestResponse;

			Assert.That(response, Is.Not.Null);
			Assert.That(response.ExecuteTimes, Is.EqualTo(1));
		}
	}
}