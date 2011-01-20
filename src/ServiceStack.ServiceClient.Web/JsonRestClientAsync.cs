using System;
using ServiceStack.Service;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
	public class JsonRestClientAsync 
		: IRestClientAsync
	{
		public const string ContentType = "application/json";

		public JsonRestClientAsync(string baseUri)
			: this()
		{
			this.BaseUri = baseUri.WithTrailingSlash();
		}

		public JsonRestClientAsync()
		{
			this.client = new AsyncServiceClient {
				ContentType = ContentType,
				StreamSerializer = JsonSerializer.SerializeToStream,
				StreamDeserializer = JsonSerializer.DeserializeFromStream
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
			this.client.SendAsync(HttpMethod.Get, GetUrl(relativeOrAbsoluteUrl), null, onSuccess, onError);
		}

		public void DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			this.client.SendAsync(HttpMethod.Delete, GetUrl(relativeOrAbsoluteUrl), null, onSuccess, onError);
		}

		public void PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			this.client.SendAsync(HttpMethod.Post, GetUrl(relativeOrAbsoluteUrl), request, onSuccess, onError);
		}

		public void PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			this.client.SendAsync(HttpMethod.Put, GetUrl(relativeOrAbsoluteUrl), request, onSuccess, onError);
		}

		public void Dispose()
		{
		}
	}
}