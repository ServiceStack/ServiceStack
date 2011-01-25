using System;
using ServiceStack.Service;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support
{
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

		public void SendAsync<TResponse>(object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			var response = default(TResponse);
			try
			{
				response = this.Send<TResponse>(request);
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