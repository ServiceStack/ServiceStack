using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ServiceStack.Common.Net30;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.ServiceModel;

namespace ServiceStack.ServiceInterface.Providers
{
	public class InMemoryRollingRequestLogger : IRequestLogger
	{
		private static int requestId = 0;

		public const int DefaultCapacity = 1000;
		private readonly ConcurrentQueue<RequestLogEntry> logEntries = new ConcurrentQueue<RequestLogEntry>();
		readonly int capacity;

		public bool EnableSessionTracking { get; set; }

		public bool EnableResponseTracking { get; set; }

		public bool EnableErrorTracking { get; set; }

		public string RequiresRole { get; set; }

		public Type[] ExcludeRequestDtoTypes { get; set; }

		public Type[] HideRequestBodyForRequestDtoTypes { get; set; }

		public InMemoryRollingRequestLogger(int? capacity = DefaultCapacity)
		{
			this.capacity = capacity.GetValueOrDefault(DefaultCapacity);
		}

		public void Log(IRequestContext requestContext, object requestDto, object response)
		{
			var requestType = requestDto != null ? requestDto.GetType() : null;

			if (ExcludeRequestDtoTypes != null
				&& requestType != null
				&& ExcludeRequestDtoTypes.Contains(requestType))
				return;
				
			var httpReq = requestContext.Get<IHttpRequest>();
			var entry = new RequestLogEntry {
				Id = Interlocked.Increment(ref requestId),
				DateTime = DateTime.UtcNow,
				HttpMethod = httpReq.HttpMethod,
				AbsoluteUri = httpReq.AbsoluteUri,
				PathInfo = httpReq.PathInfo,
				IpAddress = requestContext.IpAddress,
				ForwardedFor = httpReq.Headers[HttpHeaders.XForwardedFor],
				Referer = httpReq.Headers[HttpHeaders.Referer],
				Headers = httpReq.Headers.ToDictionary(),
				UserAuthId = httpReq.GetItemOrCookie(HttpHeaders.XUserAuthId),
				SessionId = httpReq.GetSessionId(),
				Items = httpReq.Items,
				Session = EnableSessionTracking ? httpReq.GetSession() : null,
			};

			if (HideRequestBodyForRequestDtoTypes != null
				&& requestType != null
				&& !HideRequestBodyForRequestDtoTypes.Contains(requestType)) 
			{
				entry.RequestDto = requestDto;
				entry.FormData = httpReq.FormData.ToDictionary();
			}
			if (response.IsErrorResponse()) {
				if (EnableResponseTracking)
					entry.ResponseDto = response;
			}
			else {
				if (EnableErrorTracking)
					entry.ErrorResponse = ToSerializableErrorResponse(response);
			}

			logEntries.Enqueue(entry);

			RequestLogEntry dummy;
			if (logEntries.Count > capacity)
				logEntries.TryDequeue(out dummy);
		}

		public List<RequestLogEntry> GetLatestLogs(int? take)
		{
			var requestLogEntries = logEntries.ToArray();			
			return take.HasValue 
				? new List<RequestLogEntry>(requestLogEntries.Take(take.Value))
				: new List<RequestLogEntry>(requestLogEntries);
		}

		public static object ToSerializableErrorResponse(object response)
		{
			var errorResult = response as IHttpResult;
			if (errorResult != null)
				return errorResult.Response;

			var ex = response as Exception;
			if (ex != null)
				ResponseStatusTranslator.Instance.Parse(ex);

			return null;
		}
	}
}