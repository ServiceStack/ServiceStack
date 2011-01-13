using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Service;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
	/**
	 * Need to provide async request options
	 * http://msdn.microsoft.com/en-us/library/86wf6409(VS.71).aspx
	 */
	public abstract class AsyncServiceClientBase
		: IAsyncServiceClient
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(AsyncServiceClientBase));

		public static Action<HttpWebRequest> HttpWebRequestFilter { get; set; }

		public const string DefaultHttpMethod = "POST";

		const int BufferSize = 1024;

		internal class RequestState<TResponse>
		{
			public RequestState()
			{
				BufferRead = new byte[BufferSize];
				RequestData = new StringBuilder();
				Request = null;
				ResponseStream = null;
			}

			public string Url;

			public StringBuilder RequestData;

			public byte[] BufferRead;

			public HttpWebRequest Request;

			public HttpWebResponse Response;

			public Stream ResponseStream;

			public int Completed;

			public Timer Timer;

			public Action<TResponse> OnSuccess;

			public Action<TResponse> OnError;

			public void StartTimer(TimeSpan timeOut)
			{
				this.Timer = new Timer(this.TimedOut, this, (int)timeOut.TotalMilliseconds, System.Threading.Timeout.Infinite);
			}

			public void TimedOut(object state)
			{
				if (Interlocked.Increment(ref Completed) == 1)
				{
					if (this.Request != null)
					{
						this.Request.Abort();
					}
				}
				this.Timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
				this.Timer.Dispose();
			}
		}

		protected AsyncServiceClientBase()
		{
			this.HttpMethod = DefaultHttpMethod;
			this.Timeout = TimeSpan.FromSeconds(60);
		}

		protected AsyncServiceClientBase(string syncReplyBaseUri, string asyncOneWayBaseUri)
			: this()
		{
			this.SyncReplyBaseUri = syncReplyBaseUri;
			this.AsyncOneWayBaseUri = asyncOneWayBaseUri;
		}

		public string BaseUri
		{
			set
			{
				var baseUri = value.WithTrailingSlash();
				this.SyncReplyBaseUri = baseUri + "SyncReply/";
				this.AsyncOneWayBaseUri = baseUri + "AsyncOneWay/";
			}
		}

		public string SyncReplyBaseUri { get; set; }

		public string AsyncOneWayBaseUri { get; set; }

		public TimeSpan Timeout { get; set; }

		public abstract string ContentType { get; }

		public string HttpMethod { get; set; }

		public abstract void SerializeToStream(object request, Stream stream);

		public abstract T DeserializeFromStream<T>(Stream stream);


		public void SendAsync<TResponse>(object request, Action<TResponse> onSuccess)
		{
			SendAsync(request, onSuccess, null);
		}

		public void SendAsync<TResponse>(object request, Action<TResponse> onSuccess, Action<TResponse> onError)
		{
			var requestUri = this.SyncReplyBaseUri.WithTrailingSlash() + request.GetType().Name;

			var isHttpGet = HttpMethod != null && HttpMethod.ToUpper() == "GET";
			if (isHttpGet)
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
				Request = webRequest,
				OnSuccess = onSuccess,
				OnError = onError,
			};
			requestState.StartTimer(this.Timeout);

			webRequest.Accept = string.Format("{0}, */*", ContentType);
			webRequest.Method = HttpMethod ?? DefaultHttpMethod;

			if (HttpWebRequestFilter != null)
			{
				HttpWebRequestFilter(webRequest);
			}

			if (!isHttpGet)
			{
				webRequest.ContentType = ContentType;

				//TODO: change to use: webRequest.BeginGetRequestStream()
				using (var requestStream = webRequest.GetRequestStream())
				{
					SerializeToStream(request, requestStream);
				}
			}
			var result = webRequest.BeginGetResponse(ResponseCallback<TResponse>, requestState);
		}

		private void ResponseCallback<T>(IAsyncResult asyncResult)
		{
			var requestState = (RequestState<T>)asyncResult.AsyncState;
			try
			{
				var webRequest = requestState.Request;
				requestState.Response = (HttpWebResponse)webRequest.EndGetResponse(asyncResult);

				// Read the response into a Stream object.
				var responseStream = requestState.Response.GetResponseStream();
				requestState.ResponseStream = responseStream;

				var asyncRead = responseStream.BeginRead(requestState.BufferRead, 0, BufferSize, ReadCallBack<T>, requestState);
				return;
			}
			catch (Exception e)
			{
				HandleResponseError(e, requestState);
			}
			//allDone.Set();
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

					requestState.RequestData.Append(Encoding.UTF8.GetString(requestState.BufferRead, 0, read));
					var nextAsyncResult = responseStream.BeginRead(
						requestState.BufferRead, 0, BufferSize, ReadCallBack<T>, requestState);

					return;
				}
				
				Interlocked.Increment(ref requestState.Completed);

				var response = default(T);
				try
				{
					response = DeserializeFromStream<T>(responseStream);
				}
				catch (Exception ex)
				{
					Log.Debug(string.Format("Error Reading Response Error: {0}", ex.Message), ex);
					if (requestState.OnError != null) requestState.OnError(default(T));
				}
				finally
				{
					responseStream.Close();
				}

				if (requestState.OnSuccess != null)
				{
					requestState.OnSuccess(response);
				}
			}
			catch (Exception ex)
			{
				HandleResponseError(ex, requestState);
			}
			//allDone.Set();
		}

		public void SendOneWay<TResponse>(object request, Action<TResponse> error)
		{
			throw new NotImplementedException();
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

				if (requestState.OnError == null) return;

				try
				{
					using (var stream = errorResponse.GetResponseStream())
					{
						var response = DeserializeFromStream<TResponse>(stream);
						requestState.OnError(response);
					}
				}
				catch (WebException ex)
				{
					// Oh, well, we tried
					Log.Debug(string.Format("WebException Reading Response Error: {0}", ex.Message), ex);
					requestState.OnError(default(TResponse));
				}
				return;
			}

			var authEx = exception as AuthenticationException;
			if (authEx != null)
			{
				var customEx = WebRequestUtils.CreateCustomException(requestState.Url, authEx);

				Log.Debug(string.Format("AuthenticationException: {0}", customEx.Message), customEx);
				if (requestState.OnError != null) requestState.OnError(default(TResponse));
			}

			Log.Debug(string.Format("Exception Reading Response Error: {0}", exception.Message), exception);
			if (requestState.OnError != null) requestState.OnError(default(TResponse));
		}

		public void SendOneWay(object request)
		{
			throw new NotImplementedException();
		}

		public void Dispose() { }
	}
}