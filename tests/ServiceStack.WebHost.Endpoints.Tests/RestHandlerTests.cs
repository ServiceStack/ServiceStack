using System.Collections.Specialized;
using Moq;
using NUnit.Framework;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class RestHandlerTests
	{
		[Test]
		public void Throws_binding_exception_when_unable_to_match_path_values()
		{
			var path = "/request/{will_not_match_property_id}/pathh";
			var request = ConfigureRequest(path);
			var response = new Mock<IHttpResponse>().Object;
			ConfigureHost();

			var handler = new RestHandler
			{
				RestPath = new RestPath(typeof(RequestType), path)
			};

			Assert.Throws<RequestBindingException>(() => handler.ProcessRequest(request, response, string.Empty));
		}

		[Test]
		public void Throws_binding_exception_when_unable_to_bind_request()
		{
			var path = "/request/{id}/path";
			var request = ConfigureRequest(path);
			var response = new Mock<IHttpResponse>().Object;
			ConfigureHost();

			var handler = new RestHandler
			{
				RestPath = new RestPath(typeof(RequestType), path)
			};

			Assert.Throws<RequestBindingException>(() => handler.ProcessRequest(request, response, string.Empty));
		}

		private IHttpRequest ConfigureRequest(string path)
		{
			var request = new Mock<IHttpRequest>();
			request.Expect(x => x.QueryString).Returns(new NameValueCollection());
			request.Expect(x => x.PathInfo).Returns(path);

			return request.Object;
		}

		private void ConfigureHost()
		{
			var host = ServiceHostTestBase.CreateAppHost();
			EndpointHost.ConfigureHost(host, string.Empty, new ServiceManager(typeof(RestHandlerTests).Assembly).Init());		
		}

		public class RequestType
		{
			public int Id { get; set; }
		}
	}
}
