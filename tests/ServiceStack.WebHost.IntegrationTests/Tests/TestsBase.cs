using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ServiceStack.Common.Web;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	public abstract class TestsBase
	{
		//All integration tests call the Webservices hosted at the following location:
		protected const string ServiceClientBaseUri = "http://localhost/ServiceStack.WebHost.IntegrationTests/ServiceStack";

		protected virtual IServiceClient CreateNewServiceClient()
		{
			throw new NotImplementedException();
		}

		public class DirectServiceClient : IServiceClient
		{
			ServiceManager ServiceManager { get; set; }

			public DirectServiceClient(ServiceManager serviceManager)
			{
				this.ServiceManager = serviceManager;
			}

			public void SendOneWay(object request)
			{
				ServiceManager.Execute(request);
			}

			public TResponse Send<TResponse>(object request)
			{
				var response = ServiceManager.Execute(request);
				return (TResponse)response;
			}

			public void Dispose() { }
		}

		public object ExecutePath(string pathInfo)
		{
			var qsIndex = pathInfo.IndexOf("?");
			if (qsIndex != -1)
			{
				var qs = pathInfo.Substring(qsIndex + 1);
				pathInfo = pathInfo.Substring(0, qsIndex);
				var kvps = qs.Split('&');

				var map = new Dictionary<string, string>();
				foreach (var kvp in kvps)
				{
					var parts = kvp.Split('=');
					map[parts[0]] = parts.Length > 1 ? parts[1] : null;
				}

				return ExecutePath(pathInfo, map, null, null);
			}

			return ExecutePath(pathInfo, null, null, null);
		}

		public object ExecutePath(string pathInfo,
			Dictionary<string, string> queryString,
			Dictionary<string, string> formData,
			string requestBody)
		{
			var httpHandler = ServiceStackHttpHandlerFactory.GetHandlerForPathInfo(null, pathInfo) as EndpointHandlerBase;
			if (httpHandler == null)
				throw new NotSupportedException(pathInfo);
			
			var httpMethod = formData == null ? HttpMethods.Get : HttpMethods.Post;

			var httpReq = new MockHttpRequest(
					httpHandler.RequestName, httpMethod,
					pathInfo,
					queryString.ToNameValueCollection(),
					requestBody == null ? null : new MemoryStream(Encoding.UTF8.GetBytes(requestBody)), 
					formData.ToNameValueCollection()
				);

			var response = httpHandler.CreateRequest(httpReq, httpHandler.RequestName);

			return response;
		}

	}
}