using System;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Support.Mocks;
using ServiceStack.WebHost.Endpoints.Tests.Mocks;

namespace ServiceStack.WebHost.Endpoints.Tests.Support
{
	public class DirectServiceClient : IServiceClient
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

		private bool ApplyRequestFilters(object request)
		{
			if (EndpointHost.ApplyRequestFilters(httpReq, httpRes, request))
			{
				if (httpRes.StatusCode >= 400)
				{
					throw new WebServiceException("WebServiceException, StatusCode: " + httpRes.StatusCode)
					{
						StatusCode = httpRes.StatusCode,
					};
				}
				return true;
			}
			return false;
		}

		private bool ApplyResponseFilters(object response)
		{
			if (EndpointHost.ApplyResponseFilters(httpReq, httpRes, response))
			{
				if (httpRes.StatusCode >= 400)
				{
					throw new WebServiceException("WebServiceException, StatusCode: " + httpRes.StatusCode)
					{
						ResponseDto = response,
						StatusCode = httpRes.StatusCode,
					};
				}
				return true;
			}
			return false;
		}

		public TResponse Send<TResponse>(object request)
		{
			if (ApplyRequestFilters(request)) return default(TResponse);

			var response = ServiceManager.Execute(request);

			if (ApplyResponseFilters(response)) return (TResponse)response;

			return (TResponse)response;
		}

		public void SendAsync<TResponse>(object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			var response = default(TResponse);
			try
			{
				try
				{
					if (ApplyRequestFilters(request))
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
					if (ApplyResponseFilters(request))
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
}