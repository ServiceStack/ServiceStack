using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class ServiceStackHostTests
{
	ServiceStackHost appHost;

	[OneTimeSetUp]
	public void TestFixtureSetUp()
	{
		appHost = new TestAppHost().Init();
	}

	[OneTimeTearDown]
	public void TestFixtureTearDown()
	{
		appHost.Dispose();
	}

	[Test]
	public void Can_run_nested_service()
	{
		var request = new Nested();
		var response = appHost.ExecuteService(request) as NestedResponse;

		Assert.That(response, Is.Not.Null);
	}

	[Test]
	public void Can_run_test_service()
	{
		var request = new Test();
		var response = appHost.ExecuteService(request) as TestResponse;

		Assert.That(response, Is.Not.Null);
		Assert.That(response.Foo, Is.Not.Null);
	}

	[Test]
	public void Call_AsyncOneWay_endpoint_on_TestService_calls_Execute()
	{
		TestService.ResetStats();

		var request = new Test();
		var response = appHost.ExecuteService(request, RequestAttributes.OneWay) as TestResponse;

		Assert.That(response, Is.Not.Null);
		Assert.That(response.ExecuteTimes, Is.EqualTo(1));
	}
}