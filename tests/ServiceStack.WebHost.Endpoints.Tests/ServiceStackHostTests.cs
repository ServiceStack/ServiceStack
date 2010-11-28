using System;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.ServiceHost;
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
			var response = host.ExecuteService(request, EndpointAttributes.AsyncOneWay) as TestResponse;

			Assert.That(response, Is.Not.Null);
			Assert.That(response.ExecuteTimes, Is.EqualTo(1));
			Assert.That(response.ExecuteAsyncTimes, Is.EqualTo(0));
		}

		[Test]
		public void Call_AsyncOneWay_endpoint_on_AsyncTestService_calls_ExecuteAsync()
		{
			var host = new TestAppHost();
			host.Init();

			TestAsyncService.ResetStats();

			var request = new TestAsync();
			var response = host.ExecuteService(request, EndpointAttributes.AsyncOneWay) as TestAsyncResponse;

			Assert.That(response, Is.Not.Null);
			Assert.That(response.ExecuteTimes, Is.EqualTo(0));
			Assert.That(response.ExecuteAsyncTimes, Is.EqualTo(1));
		}
	}
}