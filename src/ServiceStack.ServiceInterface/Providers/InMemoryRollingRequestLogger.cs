using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ServiceStack.Common.Net30;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.ServiceModel;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Providers
{
	public class InMemoryRollingRequestLogger : IRequestLogger
	{
		private static int requestId = 0;

		public const int DefaultCapacity = 1000;
		private readonly ConcurrentQueue<RequestLogEntry> logEntries = new ConcurrentQueue<RequestLogEntry>();
		readonly int capacity;

		public Type[] HideRequestBodyForRequestDtoTypes { get; set; }

		public InMemoryRollingRequestLogger(int? capacity = DefaultCapacity)
		{
			this.capacity = capacity.GetValueOrDefault(DefaultCapacity);
		}

		public void Log(IRequestContext requestContext, object requestDto)
		{
			var httpReq = requestContext.Get<IHttpRequest>();
			var entry = new RequestLogEntry {
				Id = Interlocked.Increment(ref requestId),
				DateTime = DateTime.UtcNow,
				HttpMethod = httpReq.HttpMethod,
				AbsoluteUri = httpReq.AbsoluteUri,
				IpAddress = requestContext.IpAddress,
				PathInfo = httpReq.PathInfo,
				Referer = httpReq.Headers[HttpHeaders.Referer],
				Headers = httpReq.Headers.ToDictionary(),
				UserAuthId = httpReq.GetItemOrCookie(HttpHeaders.XUserAuthId),
				SessionId = httpReq.GetSessionId(),
				Items = httpReq.Items,
			};

			if (HideRequestBodyForRequestDtoTypes != null
				&& requestDto != null 
				&& !HideRequestBodyForRequestDtoTypes.Contains(requestDto.GetType()))
			{
				entry.RequestDto = requestDto;
				entry.FormData = httpReq.FormData.ToDictionary();
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
	}
}