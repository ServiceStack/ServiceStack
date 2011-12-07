/*
	Note: The preferred ServiceClients are in ServiceStack.Common.dll
	https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Common/ServiceClient.Web/XmlServiceClient.cs

	This is a dependency-free ServiceClient using the built-in .NET BCL Serializers.
*/

using System;
using ServiceStack.Service;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
	public class XmlRestClientAsync 
		: IRestClientAsync
	{
		public const string ContentType = "application/xml";

		public XmlRestClientAsync(string baseUri)
			: this()
		{
			this.BaseUri = baseUri.WithTrailingSlash();
		}

		public XmlRestClientAsync()
		{
			this.client = new AsyncServiceClient {
				ContentType = ContentType,
				StreamSerializer = XmlSerializer.SerializeToStream,
				StreamDeserializer = XmlSerializer.DeserializeFromStream
			};
		}

		private readonly AsyncServiceClient client;

		public string BaseUri { get; set; }

		private string GetUrl(string relativeOrAbsoluteUrl)
		{
			return relativeOrAbsoluteUrl.StartsWith("http:")
				|| relativeOrAbsoluteUrl.StartsWith("https:")
					 ? relativeOrAbsoluteUrl
					 : this.BaseUri + relativeOrAbsoluteUrl;
		}

		public void GetAsync<TResponse>(string relativeOrAbsoluteUrl, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			this.client.SendAsync("GET", GetUrl(relativeOrAbsoluteUrl), null, onSuccess, onError);
		}

		public void DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			this.client.SendAsync("DELETE", GetUrl(relativeOrAbsoluteUrl), null, onSuccess, onError);
		}

		public void PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			this.client.SendAsync("POST", GetUrl(relativeOrAbsoluteUrl), request, onSuccess, onError);
		}

		public void PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			this.client.SendAsync("PUT", GetUrl(relativeOrAbsoluteUrl), request, onSuccess, onError);
		}

		public void Dispose()
		{
		}
	}
}