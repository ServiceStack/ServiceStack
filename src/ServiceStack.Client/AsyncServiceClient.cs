/*
	Note: The preferred ServiceClients are in ServiceStack.Common.dll
	https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Common/ServiceClient.Web/JsonServiceClient.cs

	This is a dependency-free ServiceClient using the built-in .NET BCL Serializers.
*/

using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
	/**
	 * Need to provide async request options
	 * http://msdn.microsoft.com/en-us/library/86wf6409(VS.71).aspx
	 */

	public class AsyncServiceClient
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(AsyncServiceClient));

		public static Action<HttpWebRequest> HttpWebRequestFilter { get; set; }

		const int BufferSize = 4096;

		internal class RequestState<TResponse> : IDisposable
		{
			public RequestState()
			{
				BufferRead = new byte[BufferSize];
				TextData = new StringBuilder();
				BytesData = new MemoryStream(BufferSize);
				WebRequest = null;
				ResponseStream = null;
			}

			public string Url;

			public StringBuilder TextData;

			public MemoryStream BytesData;

			public byte[] BufferRead;

			public object Request;

			public HttpWebRequest WebRequest;

			public HttpWebResponse WebResponse;

			public Stream ResponseStream;

			public int Completed;

			public Timer Timer;

			public Action<TResponse> OnSuccess;

			public Action<TResponse, Exception> OnError;

			public void HandleError(TResponse response, Exception ex)
			{
				if (OnError != null)
				{
					OnError(response, ex);
				}
			}

			public void StartTimer(TimeSpan timeOut)
			{
				this.Timer = new Timer(this.TimedOut, this, (int)timeOut.TotalMilliseconds, System.Threading.Timeout.Infinite);
			}

			public void TimedOut(object state)
			{
				if (Interlocked.Increment(ref Completed) == 1)
				{
					if (this.WebRequest != null)
					{
						this.WebRequest.Abort();
					}
				}
				this.Timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
				this.Timer.Dispose();
				this.Dispose();
			}

			public void Dispose()
			{
				if (this.BytesData == null) return;
				this.BytesData.Dispose();
				this.BytesData = null;
			}
		}

		public AsyncServiceClient()
		{
			this.Timeout = TimeSpan.FromSeconds(60);
		}

		public TimeSpan Timeout { get; set; }

		public string ContentType { get; set; }

		public StreamSerializerDelegate StreamSerializer { get; set; }

		public StreamDeserializerDelegate StreamDeserializer { get; set; }

		public void SendAsync<TResponse>(string httpMethod, string absoluteUrl, object request,
			Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			var requestState = SendWebRequest(httpMethod, absoluteUrl, request, onSuccess, onError);
		}

		private RequestState<TResponse> SendWebRequest<TResponse>(string httpMethod, string absoluteUrl, object request,
			Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			if (httpMethod == null) throw new ArgumentNullException("httpMethod");

			var requestUri = absoluteUrl;
			var httpGetOrDelete = (httpMethod == "GET" || httpMethod == "DELETE");
			var hasQueryString = request != null && httpGetOrDelete;
			if (hasQueryString)
			{
				var queryString = QueryStringSerializer.SerializeToString(request);
				if (!string.IsNullOrEmpty(queryString))
				{
					requestUri += "?" + queryString;
				}
			}

			var webRequest = (HttpWebRequest)WebRequest.Create(requestUri);

			var requestState = new RequestState<TResponse>
			{
				Url = requestUri,
				WebRequest = webRequest,
				Request = request,
				OnSuccess = onSuccess,
				OnError = onError,
			};
			requestState.StartTimer(this.Timeout);

			webRequest.Accept = string.Format("{0}, */*", ContentType);
			webRequest.Method = httpMethod;

			if (HttpWebRequestFilter != null)
			{
				HttpWebRequestFilter(webRequest);
			}

			if (!httpGetOrDelete && request != null)
			{
				webRequest.ContentType = ContentType;
				webRequest.BeginGetRequestStream(RequestCallback<TResponse>, requestState);
			}
			else
			{
				var result = requestState.WebRequest.BeginGetResponse(ResponseCallback<TResponse>, requestState);
			}

			return requestState;
		}

		private void RequestCallback<T>(IAsyncResult asyncResult)
		{
			var requestState = (RequestState<T>)asyncResult.AsyncState;
			try
			{
				var req = requestState.WebRequest;
				var postStream = req.EndGetRequestStream(asyncResult);

				StreamSerializer(requestState.Request, postStream);

				postStream.Close();

				var result = requestState.WebRequest.BeginGetResponse(ResponseCallback<T>, requestState);
			}
			catch (Exception ex)
			{
				HandleResponseError(ex, requestState);
			}
		}

		private void ResponseCallback<T>(IAsyncResult asyncResult)
		{
			var requestState = (RequestState<T>)asyncResult.AsyncState;
			try
			{
				var webRequest = requestState.WebRequest;
				requestState.WebResponse = (HttpWebResponse)webRequest.EndGetResponse(asyncResult);

				// Read the response into a Stream object.
				var responseStream = requestState.WebResponse.GetResponseStream();
				requestState.ResponseStream = responseStream;

				var asyncRead = responseStream.BeginRead(requestState.BufferRead, 0, BufferSize, ReadCallBack<T>, requestState);
				return;
			}
			catch (Exception e)
			{
				HandleResponseError(e, requestState);
			}
		}

		private void ReadCallBack<T>(IAsyncResult asyncResult)
		{
			var requestState = (RequestState<T>)asyncResult.AsyncState;
			try
			{
				var responseStream = requestState.ResponseStream;
				int read = responseStream.EndRead(asyncResult);

				if (read > 0)
				{

					requestState.BytesData.Write(requestState.BufferRead, 0, read);
					var nextAsyncResult = responseStream.BeginRead(
						requestState.BufferRead, 0, BufferSize, ReadCallBack<T>, requestState);

					return;
				}

				Interlocked.Increment(ref requestState.Completed);

				var response = default(T);
				try
				{
					requestState.BytesData.Position = 0;
					using (var reader = requestState.BytesData)
					{
						response = (T)this.StreamDeserializer(typeof(T), reader);
					}

					if (requestState.OnSuccess != null)
					{
						requestState.OnSuccess(response);
					}
				}
				catch (Exception ex)
				{
					Log.Debug(string.Format("Error Reading Response Error: {0}", ex.Message), ex);
					requestState.HandleError(default(T), ex);
				}
				finally
				{
					responseStream.Close();
				}
			}
			catch (Exception ex)
			{
				HandleResponseError(ex, requestState);
			}
		}

		private void HandleResponseError<TResponse>(Exception exception, RequestState<TResponse> requestState)
		{
			var webEx = exception as WebException;
			if (webEx != null && webEx.Status == WebExceptionStatus.ProtocolError)
			{
				var errorResponse = ((HttpWebResponse)webEx.Response);
				Log.Error(webEx);
				Log.DebugFormat("Status Code : {0}", errorResponse.StatusCode);
				Log.DebugFormat("Status Description : {0}", errorResponse.StatusDescription);

				try
				{
					using (var stream = errorResponse.GetResponseStream())
					{
						var response = (TResponse)this.StreamDeserializer(typeof(TResponse), stream);
						requestState.HandleError(response, new Exception("Web Service Exception"));
					}
				}
				catch (WebException ex)
				{
					// Oh, well, we tried
					Log.Debug(string.Format("WebException Reading Response Error: {0}", ex.Message), ex);
					requestState.HandleError(default(TResponse), ex);
				}
				return;
			}

			var authEx = exception as AuthenticationException;
			if (authEx != null)
			{
				var customEx = WebRequestUtils.CreateCustomException(requestState.Url, authEx);

				Log.Debug(string.Format("AuthenticationException: {0}", customEx.Message), customEx);
				requestState.HandleError(default(TResponse), authEx);
			}

			Log.Debug(string.Format("Exception Reading Response Error: {0}", exception.Message), exception);
			requestState.HandleError(default(TResponse), exception);
		}

		public void Dispose() { }
	}
}