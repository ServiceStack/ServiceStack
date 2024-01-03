using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class RestHandlerTests
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
	public async Task Throws_binding_exception_when_unable_to_match_path_values()
	{
		var path = "/request/{will_not_match_property_id}/pathh";
		var request = ConfigureRequest(path);
		var response = request.Response;

		var handler = new RestHandler
		{
			RestPath = new RestPath(typeof(RequestType), path)
		};

		await handler.ProcessRequestAsync(request, response, string.Empty);
		Assert.That(response.StatusCode, Is.EqualTo(400));
	}

	[Test]
	public async Task Throws_binding_exception_when_unable_to_bind_request()
	{
		var path = "/request/{id}/path";
		var request = ConfigureRequest(path);
		var response = request.Response;

		var handler = new RestHandler
		{
			RestPath = new RestPath(typeof(RequestType), path)
		};

		await handler.ProcessRequestAsync(request, response, string.Empty);
		Assert.That(response.StatusCode, Is.EqualTo(400));
	}

	private IHttpRequest ConfigureRequest(string path)
	{
		var request = new BasicHttpRequest {
			PathInfo = path
		};
		return request;
	}

	public class RequestType
	{
		public int Id { get; set; }
	}
}