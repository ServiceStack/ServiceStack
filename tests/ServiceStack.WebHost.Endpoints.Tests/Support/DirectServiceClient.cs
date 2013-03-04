using System;
using System.IO;
using System.Net;
using ServiceStack.Common.Web;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support.Mocks;
using ServiceStack.WebHost.Endpoints.Tests.Mocks;

namespace ServiceStack.WebHost.Endpoints.Tests.Support
{
	public class DirectServiceClient : IServiceClient, IRestClient
	{
		ServiceManager ServiceManager { get; set; }

		readonly HttpRequestMock httpReq = new HttpRequestMock();
		readonly HttpResponseMock httpRes = new HttpResponseMock();

		public DirectServiceClient(ServiceManager serviceManager)
		{
			this.ServiceManager = serviceManager;
		}

		public void SendOneWay(object request)
		{
			ServiceManager.Execute(request);
		}

		public void SendOneWay(string relativeOrAbsoluteUrl, object request)
		{
			ServiceManager.Execute(request);
		}

		private bool ApplyRequestFilters<TResponse>(object request)
		{
			if (EndpointHost.ApplyRequestFilters(httpReq, httpRes, request))
			{
				ThrowIfError<TResponse>(httpRes);
				return true;
			}
			return false;
		}

		private void ThrowIfError<TResponse>(HttpResponseMock httpRes)
		{
			if (httpRes.StatusCode >= 400)
			{
				var webEx = new WebServiceException("WebServiceException, StatusCode: " + httpRes.StatusCode) {
					StatusCode = httpRes.StatusCode,
					StatusDescription = httpRes.StatusDescription,
				};

				try
				{
					var deserializer = EndpointHost.AppHost.ContentTypeFilters.GetStreamDeserializer(httpReq.ResponseContentType);
					webEx.ResponseDto = deserializer(typeof(TResponse), httpRes.OutputStream);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}

				throw webEx;
			}
		}

		private bool ApplyResponseFilters<TResponse>(object response)
		{
			if (EndpointHost.ApplyResponseFilters(httpReq, httpRes, response))
			{
				ThrowIfError<TResponse>(httpRes);
				return true;
			}
			return false;
		}

		public TResponse Send<TResponse>(object request)
		{
			httpReq.HttpMethod = HttpMethods.Post;			

			if (ApplyRequestFilters<TResponse>(request)) return default(TResponse);

			var response = ServiceManager.ServiceController.Execute(request,
				new HttpRequestContext(httpReq, httpRes, request, EndpointAttributes.HttpPost));

			if (ApplyResponseFilters<TResponse>(response)) return (TResponse)response;

			return (TResponse)response;
		}

	    public TResponse Send<TResponse>(IReturn<TResponse> request)
	    {
	        throw new NotImplementedException();
	    }

	    public void Send(IReturnVoid request)
	    {
	        throw new NotImplementedException();
	    }

	    public TResponse Get<TResponse>(IReturn<TResponse> request)
	    {
	        throw new NotImplementedException();
	    }

	    public void Get(IReturnVoid request)
	    {
	        throw new NotImplementedException();
	    }

	    public TResponse Get<TResponse>(string relativeOrAbsoluteUrl)
		{
			httpReq.HttpMethod = HttpMethods.Get;

			var requestTypeName = typeof(TResponse).Namespace + "." + relativeOrAbsoluteUrl;
			var requestType = typeof (TResponse).Assembly.GetType(requestTypeName);
			if (requestType == null)
				throw new ArgumentException("Type not found: " + requestTypeName);

			var request = requestType.CreateInstance();

			if (ApplyRequestFilters<TResponse>(request)) return default(TResponse);

			var response = ServiceManager.ServiceController.Execute(request,
				new HttpRequestContext(httpReq, httpRes, request, EndpointAttributes.HttpGet));

			if (ApplyResponseFilters<TResponse>(response)) return (TResponse)response;

			return (TResponse)response;
		}

	    public TResponse Delete<TResponse>(IReturn<TResponse> request)
	    {
	        throw new NotImplementedException();
	    }

	    public void Delete(IReturnVoid request)
	    {
	        throw new NotImplementedException();
	    }

	    public TResponse Delete<TResponse>(string relativeOrAbsoluteUrl)
		{
			throw new NotImplementedException();
		}

	    public TResponse Post<TResponse>(IReturn<TResponse> request)
	    {
	        throw new NotImplementedException();
	    }

	    public void Post(IReturnVoid request)
	    {
	        throw new NotImplementedException();
	    }

	    public TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object request)
		{
			throw new NotImplementedException();
		}

	    public TResponse Put<TResponse>(IReturn<TResponse> request)
	    {
	        throw new NotImplementedException();
	    }

	    public void Put(IReturnVoid request)
	    {
	        throw new NotImplementedException();
	    }

	    public TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object request)
		{
			throw new NotImplementedException();
		}

	    public TResponse Patch<TResponse>(IReturn<TResponse> request)
	    {
	        throw new NotImplementedException();
	    }

	    public void Patch(IReturnVoid request)
	    {
	        throw new NotImplementedException();
	    }

	    public TResponse Patch<TResponse>(string relativeOrAbsoluteUrl, object request)
	    {
	        throw new NotImplementedException();
	    }

	    public TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, string mimeType)
		{
			throw new NotImplementedException();
		}

	    public void CustomMethod(string httpVerb, IReturnVoid request)
	    {
	        throw new NotImplementedException();
	    }

	    public TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> request)
	    {
	        throw new NotImplementedException();
	    }

	    public HttpWebResponse Head(IReturn request)
	    {
	        throw new NotImplementedException();
	    }

	    public HttpWebResponse Head(string relativeOrAbsoluteUrl)
	    {
	        throw new NotImplementedException();
	    }

	    public TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileInfo, string mimeType)
        {
            throw new NotImplementedException();
        }

		public void SendAsync<TResponse>(object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			var response = default(TResponse);
			try
			{
				try
				{
					if (ApplyRequestFilters<TResponse>(request))
					{
						onSuccess(default(TResponse));
						return;
					}
				}
				catch (Exception ex)
				{
					onError(default(TResponse), ex);
					return;
				}

				response = this.Send<TResponse>(request);

				try
				{
					if (ApplyResponseFilters<TResponse>(request))
					{
						onSuccess(response);
						return;
					}
				}
				catch (Exception ex)
				{
					onError(response, ex);
					return;
				}

				onSuccess(response);
			}
			catch (Exception ex)
			{
				if (onError != null)
				{
					onError(response, ex);
					return;
				}
				Console.WriteLine("Error: " + ex.Message);
			}
		}

		public void SetCredentials(string userName, string password)
		{
			throw new NotImplementedException();
		}

	    public void GetAsync<TResponse>(IReturn<TResponse> request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
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

	    public void DeleteAsync<TResponse>(IReturn<TResponse> request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
	    {
	        throw new NotImplementedException();
	    }

	    public void PostAsync<TResponse>(IReturn<TResponse> request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
	    {
	        throw new NotImplementedException();
	    }

	    public void PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			throw new NotImplementedException();
		}

	    public void PutAsync<TResponse>(IReturn<TResponse> request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
	    {
	        throw new NotImplementedException();
	    }

	    public void PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			throw new NotImplementedException();
		}

	    public void CustomMethodAsync<TResponse>(string httpVerb, IReturn<TResponse> request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
	    {
	        throw new NotImplementedException();
	    }

	    public void CancelAsync()
	    {
	        throw new NotImplementedException();
	    }

	    public void Dispose() { }
        public TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, object request)
	    {
	        throw new NotImplementedException();
	    }

        public TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request)
	    {
	        throw new NotImplementedException();
	    }
	}
}