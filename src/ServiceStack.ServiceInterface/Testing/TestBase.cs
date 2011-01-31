using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using Funq;
using ServiceStack.Common.Web;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.ServiceInterface.Testing
{
	public abstract class TestsBase
	{
		protected TestsBase(params Assembly[] serviceAssemblies)
			: this(null, serviceAssemblies)
		{
		}

		protected TestsBase(string serviceClientBaseUri, params Assembly[] serviceAssemblies)
		{
			ServiceClientBaseUri = serviceClientBaseUri;
			ServiceAssemblies = serviceAssemblies;

			EndpointHost.Config = new EndpointHostConfig
			{
				ServiceName = GetType().Name,
				ServiceManager = new ServiceManager(true, ServiceAssemblies),
			};
		}

		protected Container Container
		{
			get { return EndpointHost.ServiceManager.Container; }
		}

		//All integration tests call the Webservices hosted at the following location:
		protected string ServiceClientBaseUri { get; set; }
		protected Assembly[] ServiceAssemblies { get; set; }

		public virtual void OnBeforeEachTest()
		{
			EndpointHost.ServiceManager = new ServiceManager(true, ServiceAssemblies);
		}

		protected virtual IServiceClient CreateNewServiceClient()
		{
			return new DirectServiceClient(EndpointHost.ServiceManager);
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

			public TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, string mimeType)
			{
				throw new NotImplementedException();
			}

			public void SendAsync<TResponse>(object request, 
				Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
			{
				throw new NotImplementedException();
			}

			public void GetAsync<TResponse>(string relativeOrAbsoluteUrl, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
			{
				throw new NotImplementedException();
			}

			public void DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
			{
				throw new NotImplementedException();
			}

			public void PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
			{
				throw new NotImplementedException();
			}

			public void PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
			{
				throw new NotImplementedException();
			}

			public void Dispose() { }
		}

		public object ExecutePath(string pathInfo)
		{
			return ExecutePath(HttpMethods.Get, pathInfo);
		}

		private class UrlParts
		{
			public UrlParts(string pathInfo)
			{
				this.PathInfo = pathInfo;
				var qsIndex = pathInfo.IndexOf("?");
				if (qsIndex != -1)
				{
					var qs = pathInfo.Substring(qsIndex + 1);
					this.PathInfo = pathInfo.Substring(0, qsIndex);
					var kvps = qs.Split('&');

					this.QueryString = new Dictionary<string, string>();
					foreach (var kvp in kvps)
					{
						var parts = kvp.Split('=');
						this.QueryString[parts[0]] = parts.Length > 1 ? parts[1] : null;
					}
				}
			}

			public string PathInfo { get; private set; }
			public Dictionary<string, string> QueryString { get; private set; }
		}

		public object ExecutePath(string httpMethod, string pathInfo)
		{
			var urlParts = new UrlParts(pathInfo);
			return ExecutePath(httpMethod, urlParts.PathInfo, urlParts.QueryString, null, null);
		}

		public object ExecutePath<T>(
			string httpMethod,
			string pathInfo,
			Dictionary<string, string> queryString,
			Dictionary<string, string> formData,
			T requestBody) where T : class
		{
			var json = requestBody != null ? JsonSerializer.SerializeToString(requestBody) : null;
			return ExecutePath(httpMethod, pathInfo, queryString, formData, json);
		}

		public object ExecutePath(
			string httpMethod,
			string pathInfo,
			Dictionary<string, string> queryString,
			Dictionary<string, string> formData,
			string requestBody)
		{
			var httpHandler = GetHandler(httpMethod, pathInfo);

			var contentType = (formData != null && formData.Count > 0)
				? ContentType.FormUrlEncoded
				: requestBody != null ? ContentType.Json : null;

			var httpReq = new MockHttpRequest(
					httpHandler.RequestName, httpMethod, contentType,
					pathInfo,
					queryString.ToNameValueCollection(),
					requestBody == null ? null : new MemoryStream(Encoding.UTF8.GetBytes(requestBody)),
					formData.ToNameValueCollection()
				);

			var request = httpHandler.CreateRequest(httpReq, httpHandler.RequestName);
			var response = httpHandler.GetResponse(httpReq, request);

			return response;
		}

		public object GetRequest(string pathInfo)
		{
			var urlParts = new UrlParts(pathInfo);
			return GetRequest(HttpMethods.Get, urlParts.PathInfo, urlParts.QueryString, null, null);
		}

		public object GetRequest(string httpMethod, string pathInfo)
		{
			var urlParts = new UrlParts(pathInfo);
			return GetRequest(httpMethod, urlParts.PathInfo, urlParts.QueryString, null, null);
		}

		public object GetRequest(
				string httpMethod,
				string pathInfo,
				Dictionary<string, string> queryString,
				Dictionary<string, string> formData,
				string requestBody)
		{
			var httpHandler = GetHandler(httpMethod, pathInfo);

			var contentType = (formData != null && formData.Count > 0)
				? ContentType.FormUrlEncoded
				: requestBody != null ? ContentType.Json : null;

			var httpReq = new MockHttpRequest(
					httpHandler.RequestName, httpMethod, contentType,
					pathInfo,
					queryString.ToNameValueCollection(),
					requestBody == null ? null : new MemoryStream(Encoding.UTF8.GetBytes(requestBody)),
					formData.ToNameValueCollection()
				);

			var request = httpHandler.CreateRequest(httpReq, httpHandler.RequestName);
			return request;
		}

		private static EndpointHandlerBase GetHandler(string httpMethod, string pathInfo)
		{
			var httpHandler = ServiceStackHttpHandlerFactory.GetHandlerForPathInfo(httpMethod, pathInfo) as EndpointHandlerBase;
			if (httpHandler == null)
				throw new NotSupportedException(pathInfo);
			return httpHandler;
		}
	}

}